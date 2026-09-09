using System.Text;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;

namespace FlowRide.Poc.OutboxRabbit.Outbox;

public static class RabbitTopology
{
    public const string QueueName = "flowride.outbox";
}

// ADR-20, alternativa 2 ("Transactional Outbox + Message Relay"): antes de publicar,
// reclama las filas con "SELECT ... FOR UPDATE SKIP LOCKED" dentro de la misma
// transacción. Esa reclama atómica es lo que evita que dos instancias relayen el
// mismo mensaje dos veces, a diferencia de la rama poc/outbox-polling.
public class RabbitOutboxPublisher(
    IServiceScopeFactory scopeFactory,
    IConnection rabbitConnection,
    ILogger<RabbitOutboxPublisher> logger) : BackgroundService
{
    private readonly string _instancia = $"{Environment.MachineName}:{Environment.ProcessId}:{Environment.GetEnvironmentVariable("INSTANCE_NAME") ?? "?"}";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using var channel = await rabbitConnection.CreateChannelAsync(cancellationToken: stoppingToken);
        await channel.QueueDeclareAsync(RabbitTopology.QueueName, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);

        logger.LogInformation("RabbitOutboxPublisher iniciado en instancia {Instancia}", _instancia);

        while (!stoppingToken.IsCancellationRequested)
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<FlowRideDbContext>();

            await using var tx = await db.Database.BeginTransactionAsync(stoppingToken);

            var reclamados = await db.OutboxMessages
                .FromSqlRaw("""
                    SELECT * FROM "OutboxMessages"
                    WHERE "ProcesadoEn" IS NULL
                    ORDER BY "CreadoEn"
                    LIMIT 10
                    FOR UPDATE SKIP LOCKED
                    """)
                .ToListAsync(stoppingToken);

            foreach (var mensaje in reclamados)
            {
                var body = Encoding.UTF8.GetBytes(mensaje.Payload);
                await channel.BasicPublishAsync(
                    exchange: string.Empty,
                    routingKey: RabbitTopology.QueueName,
                    mandatory: true,
                    body: body,
                    cancellationToken: stoppingToken);

                mensaje.ProcesadoEn = DateTimeOffset.UtcNow;
                mensaje.ProcesadoPor = $"relay:{_instancia}";
                logger.LogInformation("[{Instancia}] Relayado OutboxMessage {Id} ({Tipo}) a RabbitMQ", _instancia, mensaje.Id, mensaje.Tipo);
            }

            await db.SaveChangesAsync(stoppingToken);
            await tx.CommitAsync(stoppingToken);

            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }
}
