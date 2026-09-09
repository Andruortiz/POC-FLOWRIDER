using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace FlowRide.Poc.WhatsAppMeta.Notificaciones;

// ADR-23: lado Meta Cloud API directa de la comparación (ver rama
// poc/whatsapp-twilio para el otro lado). Lee META_ACCESS_TOKEN y
// META_PHONE_NUMBER_ID del entorno; sin ellos, falla con un mensaje claro.
// No hay SDK oficial de Meta para .NET, por eso va directo contra la Graph API REST.
public class MetaCloudApiWhatsAppSender(IHttpClientFactory httpClientFactory, IConfiguration configuration) : IWhatsAppTemplateSender
{
    private const string GraphApiVersion = "v20.0";

    public async Task<EnvioResultado> EnviarPlantillaAsync(string numeroDestino, string plantilla, IReadOnlyList<string> parametros, CancellationToken cancellationToken)
    {
        var accessToken = configuration["META_ACCESS_TOKEN"];
        var phoneNumberId = configuration["META_PHONE_NUMBER_ID"];

        if (string.IsNullOrWhiteSpace(accessToken) || string.IsNullOrWhiteSpace(phoneNumberId))
        {
            return new EnvioResultado(false, null, "Faltan META_ACCESS_TOKEN / META_PHONE_NUMBER_ID en el entorno.", TimeSpan.Zero);
        }

        var client = httpClientFactory.CreateClient(nameof(MetaCloudApiWhatsAppSender));
        client.BaseAddress = new Uri("https://graph.facebook.com/");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var body = new
        {
            messaging_product = "whatsapp",
            to = numeroDestino,
            type = "template",
            template = new
            {
                name = plantilla,
                language = new { code = "es" },
                components = new object[]
                {
                    new
                    {
                        type = "body",
                        parameters = parametros.Select(p => new { type = "text", text = p }),
                    },
                },
            },
        };

        var stopwatch = Stopwatch.StartNew();
        try
        {
            var response = await client.PostAsJsonAsync($"{GraphApiVersion}/{phoneNumberId}/messages", body, cancellationToken);
            var contenido = await response.Content.ReadAsStringAsync(cancellationToken);
            stopwatch.Stop();

            if (!response.IsSuccessStatusCode)
            {
                return new EnvioResultado(false, null, $"HTTP {(int)response.StatusCode}: {contenido}", stopwatch.Elapsed);
            }

            using var json = JsonDocument.Parse(contenido);
            var idMensaje = json.RootElement.GetProperty("messages")[0].GetProperty("id").GetString();
            return new EnvioResultado(true, idMensaje, null, stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new EnvioResultado(false, null, ex.Message, stopwatch.Elapsed);
        }
    }
}
