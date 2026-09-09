using System.Diagnostics;
using Twilio.Clients;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace FlowRide.Poc.WhatsAppTwilio.Notificaciones;

// ADR-23: lado Twilio de la comparación Twilio vs. Meta Cloud API (ver rama
// poc/whatsapp-meta para el otro lado). Lee las credenciales del entorno
// (TWILIO_ACCOUNT_SID / TWILIO_AUTH_TOKEN / TWILIO_WHATSAPP_FROM) — sin ellas,
// falla con un mensaje claro en vez de una excepción críptica.
public class TwilioWhatsAppSender(IConfiguration configuration) : IWhatsAppTemplateSender
{
    public async Task<EnvioResultado> EnviarPlantillaAsync(string numeroDestino, string plantilla, IReadOnlyList<string> parametros, CancellationToken cancellationToken)
    {
        var accountSid = configuration["TWILIO_ACCOUNT_SID"];
        var authToken = configuration["TWILIO_AUTH_TOKEN"];
        var from = configuration["TWILIO_WHATSAPP_FROM"];

        if (string.IsNullOrWhiteSpace(accountSid) || string.IsNullOrWhiteSpace(authToken) || string.IsNullOrWhiteSpace(from))
        {
            return new EnvioResultado(false, null, "Faltan TWILIO_ACCOUNT_SID / TWILIO_AUTH_TOKEN / TWILIO_WHATSAPP_FROM en el entorno.", TimeSpan.Zero);
        }

        ITwilioRestClient client = new TwilioRestClient(accountSid, authToken);
        var cuerpoPlantilla = string.Format(plantilla, parametros.ToArray());

        var stopwatch = Stopwatch.StartNew();
        try
        {
            var mensaje = await MessageResource.CreateAsync(
                from: new PhoneNumber($"whatsapp:{from}"),
                to: new PhoneNumber($"whatsapp:{numeroDestino}"),
                body: cuerpoPlantilla,
                client: client);

            stopwatch.Stop();
            return new EnvioResultado(true, mensaje.Sid, null, stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new EnvioResultado(false, null, ex.Message, stopwatch.Elapsed);
        }
    }
}
