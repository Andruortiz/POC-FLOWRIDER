using Microsoft.EntityFrameworkCore;

namespace FlowRide.Poc.OutboxPolling.Outbox;

public static class OutboxEndpoints
{
    public static void MapOutboxEndpoints(this WebApplication app)
    {
        app.MapGet("/outbox", async (FlowRideDbContext db) =>
            await db.OutboxMessages.AsNoTracking().OrderBy(m => m.CreadoEn).ToListAsync());
    }
}
