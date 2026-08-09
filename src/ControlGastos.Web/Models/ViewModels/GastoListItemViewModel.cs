namespace ControlGastos.Web.Models.ViewModels;

public sealed class GastoListItemViewModel
{
    public long IdGasto { get; init; }
    public int IdCategoriaGasto { get; init; }
    public string NombreCategoria { get; init; } = string.Empty;
    public int? IdMedioPago { get; init; }
    public string? NombreMedioPago { get; init; }
    public decimal Monto { get; init; }
    public DateTimeOffset FechaGasto { get; init; }
    public string? NombreComercio { get; init; }
    public string? Descripcion { get; init; }
    public string CodEstado { get; init; } = string.Empty;
    public string CodOrigen { get; init; } = string.Empty;
    public bool TieneComprobante { get; init; }
}
