using System.Text.Json;
using FlowRide.Poc.OutboxPolling.Outbox;
using Microsoft.EntityFrameworkCore;

namespace FlowRide.Poc.OutboxPolling.Conductores;

public static class ConductoresEndpoints
{
    public static void MapConductoresEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/conductores");

        group.MapGet("/", async (FlowRideDbContext db) =>
            await db.Conductores.AsNoTracking().ToListAsync());

        group.MapPost("/", async (CrearConductorRequest request, FlowRideDbContext db) =>
        {
            var conductor = new Conductor { Id = Guid.NewGuid(), Nombre = request.Nombre };
            db.Conductores.Add(conductor);
            await db.SaveChangesAsync();
            return TypedResults.Created($"/conductores/{conductor.Id}", conductor);
        });

        group.MapPut("/{id:guid}/estado", async (Guid id, CambiarEstadoRequest request, FlowRideDbContext db) =>
        {
            var conductor = await db.Conductores.FindAsync(id);
            if (conductor is null)
            {
                return Results.NotFound();
            }

            var estadoAnterior = conductor.Estado;
            conductor.Estado = request.Estado;

            // Patrón Outbox transaccional (ADR-06): el evento se escribe en la misma
            // transacción que el cambio de estado, nunca como una llamada aparte.
            db.OutboxMessages.Add(new OutboxMessage
            {
                Id = Guid.NewGuid(),
                Tipo = "ConductorEstadoCambiado",
                Payload = JsonSerializer.Serialize(new
                {
                    conductorId = conductor.Id,
                    estadoAnterior,
                    estadoNuevo = conductor.Estado,
                }),
                CreadoEn = DateTimeOffset.UtcNow,
            });

            await db.SaveChangesAsync();
            return Results.Ok(conductor);
        });
    }
}

public record CrearConductorRequest(string Nombre);

public record CambiarEstadoRequest(EstadoConductor Estado);
