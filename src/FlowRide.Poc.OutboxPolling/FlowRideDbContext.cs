using FlowRide.Poc.OutboxPolling.Conductores;
using FlowRide.Poc.OutboxPolling.Outbox;
using Microsoft.EntityFrameworkCore;

namespace FlowRide.Poc.OutboxPolling;

public class FlowRideDbContext(DbContextOptions<FlowRideDbContext> options) : DbContext(options)
{
    public DbSet<Conductor> Conductores => Set<Conductor>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Conductor>(entity =>
        {
            entity.Property(c => c.Estado).HasConversion<string>();
        });
    }
}
