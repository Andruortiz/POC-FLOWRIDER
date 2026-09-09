using FlowRide.Poc.WhatsAppTwilio.Notificaciones;

CargarDotEnv(Path.Combine(Directory.GetCurrentDirectory(), ".env"));

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

// .NET no carga archivos .env por sí solo (a diferencia de Node): esto los vuelca
// como variables de entorno del proceso antes de armar el builder, para que
// IConfiguration las recoja igual que cualquier otra env var.
static void CargarDotEnv(string ruta)
{
    if (!File.Exists(ruta))
    {
        return;
    }

    foreach (var linea in File.ReadAllLines(ruta))
    {
        var trimmed = linea.Trim();
        if (trimmed.Length == 0 || trimmed.StartsWith('#'))
        {
            continue;
        }

        var separador = trimmed.IndexOf('=');
        if (separador < 0)
        {
            continue;
        }

        var clave = trimmed[..separador].Trim();
        var valor = trimmed[(separador + 1)..].Trim();
        Environment.SetEnvironmentVariable(clave, valor);
    }
}
