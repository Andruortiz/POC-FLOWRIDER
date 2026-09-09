using FlowRide.Poc.OutboxPolling;
using FlowRide.Poc.OutboxPolling.Conductores;
using FlowRide.Poc.OutboxPolling.Outbox;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));

builder.Services.AddDbContext<FlowRideDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

builder.Services.AddHostedService<PollingOutboxRelay>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FlowRideDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapConductoresEndpoints();
app.MapOutboxEndpoints();

app.Run();
