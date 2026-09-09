using Microsoft.EntityFrameworkCore;

namespace FlowRide.Poc.OutboxPolling.Outbox;

// ADR-20, alternativa 1: polling in-process sin locking adicional a nivel de fila.
// A propósito no usa "SELECT ... FOR UPDATE SKIP LOCKED": el objetivo de esta PoC
// es exponer la condición de carrera que el ADR describe cuando corren varias
// instancias del monolito contra la misma base de datos. Comparar contra la rama
// poc/outbox-rabbit, que sí reclama las filas antes de procesarlas.
public class PollingOutboxRelay(
    IServiceScopeFactory scopeFactory,
    ILogger<PollingOutboxRelay> logger) : BackgroundService
{
    private readonly string _instancia = $"{Environment.MachineName}:{Environment.ProcessId}:{Environment.GetEnvironmentVariable("INSTANCE_NAME") ?? "?"}";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("PollingOutboxRelay iniciado en instancia {Instancia}", _instancia);

        while (!stoppingToken.IsCancellationRequested)
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<FlowRideDbContext>();

            var pendientes = await db.OutboxMessages
                .Where(m => m.ProcesadoEn == null)
                .OrderBy(m => m.CreadoEn)
                .Take(10)
                .ToListAsync(stoppingToken);

            foreach (var mensaje in pendientes)
            {
                // Ventana de carrera deliberada: leer -> "procesar" -> marcar, sin
                // reclamar la fila antes. Si otra instancia lee el mismo mensaje
                // en este intervalo, ambas lo procesan.
                logger.LogWarning("[{Instancia}] Procesando OutboxMessage {Id} ({Tipo})", _instancia, mensaje.Id, mensaje.Tipo);
                await Task.Delay(200, stoppingToken);

                mensaje.ProcesadoEn = DateTimeOffset.UtcNow;
                mensaje.ProcesadoPor = _instancia;
                await db.SaveChangesAsync(stoppingToken);
            }

            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }
}
