namespace ControlGastos.Core.DTOs;

public sealed class DashboardResumenMensual_DTO
{
    public decimal? IngresoMensual { get; init; }
    public decimal? PresupuestoMensual { get; init; }
    public decimal TotalGastado { get; init; }
    public decimal? DisponibleEstimado { get; init; }
    public decimal? PorcentajePresupuestoConsumido { get; init; }
    public decimal PorcentajeAlerta { get; init; } = 80;
    public int CantidadGastos { get; init; }
    public decimal PromedioGasto { get; init; }
    public string? CategoriaMayorGasto { get; init; }
    public decimal MontoCategoriaMayorGasto { get; init; }
    public IReadOnlyList<DashboardCategoria_DTO> GastosPorCategoria { get; init; } = [];
    public IReadOnlyList<DashboardUltimoGasto_DTO> UltimosGastos { get; init; } = [];
}

public sealed class DashboardCategoria_DTO
{
    public string NombreCategoria { get; init; } = string.Empty;
    public decimal Monto { get; init; }
    public int Cantidad { get; init; }
}

public sealed class DashboardUltimoGasto_DTO
{
    public long IdGasto { get; init; }
    public DateTimeOffset FechaGasto { get; init; }
    public string NombreCategoria { get; init; } = string.Empty;
    public string? NombreComercio { get; init; }
    public decimal Monto { get; init; }
    public bool TieneComprobante { get; init; }
}
