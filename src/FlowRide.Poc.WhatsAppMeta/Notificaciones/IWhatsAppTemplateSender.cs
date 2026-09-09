namespace FlowRide.Poc.WhatsAppMeta.Notificaciones;

public interface IWhatsAppTemplateSender
{
    Task<EnvioResultado> EnviarPlantillaAsync(string numeroDestino, string plantilla, IReadOnlyList<string> parametros, CancellationToken cancellationToken);
}

public record EnvioResultado(bool Exitoso, string? IdMensajeProveedor, string? Error, TimeSpan Duracion);
