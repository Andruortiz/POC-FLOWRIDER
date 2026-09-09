namespace FlowRide.Poc.OutboxPolling.Outbox;

public class OutboxMessage
{
    public Guid Id { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTimeOffset CreadoEn { get; set; }
    public DateTimeOffset? ProcesadoEn { get; set; }
    public string? ProcesadoPor { get; set; }
}
