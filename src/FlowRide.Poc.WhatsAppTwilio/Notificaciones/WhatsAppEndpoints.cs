namespace FlowRide.Poc.WhatsAppTwilio.Notificaciones;

public static class WhatsAppEndpoints
{
    public static void MapWhatsAppEndpoints(this WebApplication app)
    {
        app.MapPost("/notificaciones/whatsapp/enviar-prueba", async (
            EnviarPruebaRequest request,
            TwilioWhatsAppSender sender,
            ILogger<Program> logger,
            CancellationToken cancellationToken) =>
        {
            var resultado = await sender.EnviarPlantillaAsync(request.Numero, request.Plantilla, request.Parametros, cancellationToken);
            logger.LogInformation(
                "[ADR-23] Proveedor=twilio Exitoso={Exitoso} Duracion={DuracionMs}ms Error={Error}",
                resultado.Exitoso, resultado.Duracion.TotalMilliseconds, resultado.Error);

            return resultado.Exitoso ? Results.Ok(resultado) : Results.BadRequest(resultado);
        });

        app.MapPost("/webhooks/whatsapp", async (HttpRequest httpRequest, ILogger<Program> logger) =>
        {
            using var reader = new StreamReader(httpRequest.Body);
            var payload = await reader.ReadToEndAsync();
            logger.LogInformation("[ADR-23] Webhook entrante de WhatsApp (RF-14/RF-16 simulado): {Payload}", payload);
            return Results.Ok();
        });
    }
}

public record EnviarPruebaRequest(string Numero, string Plantilla, List<string> Parametros);
