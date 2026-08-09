namespace ControlGastos.Web.Models.ViewModels;

public sealed class GastoApiRequest
{
    public int IdCategoriaGasto { get; init; }
    public int? IdMedioPago { get; init; }
    public decimal Monto { get; init; }
    public DateTimeOffset FechaGasto { get; init; }
    public string? NombreComercio { get; init; }
    public string? Descripcion { get; init; }
}
