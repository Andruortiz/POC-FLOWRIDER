using FlowRide.Poc.OutboxRabbit;
using FlowRide.Poc.OutboxRabbit.Conductores;
using FlowRide.Poc.OutboxRabbit.Outbox;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));

builder.Services.AddDbContext<FlowRideDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

builder.Services.AddSingleton<IConnection>(_ =>
{
    var factory = new ConnectionFactory
    {
        Uri = new Uri(builder.Configuration.GetConnectionString("RabbitMq") ?? "amqp://guest:guest@localhost:5672"),
    };
    return factory.CreateConnectionAsync().GetAwaiter().GetResult();
});

builder.Services.AddHostedService<RabbitOutboxPublisher>();
builder.Services.AddHostedService<RabbitOutboxConsumer>();

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
