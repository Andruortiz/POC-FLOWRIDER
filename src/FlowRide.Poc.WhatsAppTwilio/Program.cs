using FlowRide.Poc.WhatsAppTwilio.Notificaciones;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddScoped<TwilioWhatsAppSender>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapWhatsAppEndpoints();

app.Run();
