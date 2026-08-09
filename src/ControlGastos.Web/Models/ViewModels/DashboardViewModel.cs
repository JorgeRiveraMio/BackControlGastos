namespace ControlGastos.Web.Models.ViewModels;

public sealed class DashboardViewModel
{
    public int Anio { get; init; }
    public int Mes { get; init; }
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
    public IReadOnlyList<DashboardCategoriaViewModel> GastosPorCategoria { get; init; } = [];
    public IReadOnlyList<DashboardUltimoGastoViewModel> UltimosGastos { get; init; } = [];
    public bool PerfilConfigurado => IngresoMensual.HasValue || PresupuestoMensual.HasValue;
}

public sealed class DashboardResumenApiModel
{
    public decimal? IngresoMensual { get; init; }
    public decimal? PresupuestoMensual { get; init; }
    public decimal TotalGastado { get; init; }
    public decimal? DisponibleEstimado { get; init; }
    public decimal? PorcentajePresupuestoConsumido { get; init; }
    public decimal PorcentajeAlerta { get; init; }
    public int CantidadGastos { get; init; }
    public decimal PromedioGasto { get; init; }
    public string? CategoriaMayorGasto { get; init; }
    public decimal MontoCategoriaMayorGasto { get; init; }
    public IReadOnlyList<DashboardCategoriaViewModel> GastosPorCategoria { get; init; } = [];
    public IReadOnlyList<DashboardUltimoGastoViewModel> UltimosGastos { get; init; } = [];
}

public sealed class DashboardCategoriaViewModel { public string NombreCategoria { get; init; } = string.Empty; public decimal Monto { get; init; } public int Cantidad { get; init; } }
public sealed class DashboardUltimoGastoViewModel { public long IdGasto { get; init; } public DateTimeOffset FechaGasto { get; init; } public string NombreCategoria { get; init; } = string.Empty; public string? NombreComercio { get; init; } public decimal Monto { get; init; } public bool TieneComprobante { get; init; } }
