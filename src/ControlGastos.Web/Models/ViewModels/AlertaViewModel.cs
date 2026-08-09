namespace ControlGastos.Web.Models.ViewModels;

public sealed class AlertaViewModel
{
    public long IdAlerta { get; init; }
    public string CodigoTipoAlerta { get; init; } = string.Empty;
    public string Mensaje { get; init; } = string.Empty;
    public bool Leida { get; init; }
    public DateTimeOffset FechaRegistro { get; init; }
}
