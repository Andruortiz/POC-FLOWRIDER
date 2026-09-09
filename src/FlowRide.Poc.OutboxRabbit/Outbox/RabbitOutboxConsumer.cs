using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace FlowRide.Poc.OutboxRabbit.Outbox;

// Lado "consumidor" del patrón Transactional Outbox + Message Relay: compite por
// mensajes de la cola (competing consumers). Aunque corran N instancias, RabbitMQ
// entrega cada mensaje a un solo consumidor y espera el ack antes de darlo por hecho.
public class RabbitOutboxConsumer(
    IConnection rabbitConnection,
    ILogger<RabbitOutboxConsumer> logger) : BackgroundService
{
    private readonly string _instancia = $"{Environment.MachineName}:{Environment.ProcessId}:{Environment.GetEnvironmentVariable("INSTANCE_NAME") ?? "?"}";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var channel = await rabbitConnection.CreateChannelAsync(cancellationToken: stoppingToken);
        await channel.QueueDeclareAsync(RabbitTopology.QueueName, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);
        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            var payload = Encoding.UTF8.GetString(ea.Body.ToArray());
            logger.LogWarning("[{Instancia}] Consumido mensaje de RabbitMQ: {Payload}", _instancia, payload);
            await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
        };

        await channel.BasicConsumeAsync(RabbitTopology.QueueName, autoAck: false, consumer, cancellationToken: stoppingToken);
        logger.LogInformation("RabbitOutboxConsumer escuchando en instancia {Instancia}", _instancia);

        await Task.Delay(Timeout.Infinite, stoppingToken).ContinueWith(_ => { }, TaskScheduler.Default);
    }
}
