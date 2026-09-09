using FlowRide.Poc.WhatsAppMeta.Notificaciones;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddHttpClient();
builder.Services.AddScoped<MetaCloudApiWhatsAppSender>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapWhatsAppEndpoints();

app.Run();
