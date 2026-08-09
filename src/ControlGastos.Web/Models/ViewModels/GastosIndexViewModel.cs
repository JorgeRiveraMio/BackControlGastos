namespace ControlGastos.Web.Models.ViewModels;

public sealed class GastosIndexViewModel
{
    public IReadOnlyList<GastoListItemViewModel> Gastos { get; init; } = [];
    public int Anio { get; init; }
    public int Mes { get; init; }
    public decimal TotalMes { get; init; }
}
