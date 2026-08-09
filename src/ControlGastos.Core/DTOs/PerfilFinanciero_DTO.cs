namespace ControlGastos.Core.DTOs;

public sealed class PerfilFinanciero_DTO
{
    public string NombreUsuario { get; init; } = string.Empty;
    public string CodigoMoneda { get; init; } = string.Empty;
    public string ZonaHoraria { get; init; } = string.Empty;
    public decimal? IngresoMensual { get; init; }
    public decimal? PresupuestoMensual { get; init; }
    public decimal PorcentajeAlerta { get; init; }
}
