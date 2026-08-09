namespace ControlGastos.Core.DTOs;

public sealed class PerfilFinanciero_Actualizar_DTO
{
    public string NombreUsuario { get; init; } = string.Empty;
    public decimal? IngresoMensual { get; init; }
    public decimal? PresupuestoMensual { get; init; }
    public decimal PorcentajeAlerta { get; init; }
}
