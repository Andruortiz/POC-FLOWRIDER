namespace FlowRide.Poc.OutboxPolling.Conductores;

public enum EstadoConductor
{
    Disponible,
    Ocupado,
}

public class Conductor
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public EstadoConductor Estado { get; set; } = EstadoConductor.Disponible;
}
