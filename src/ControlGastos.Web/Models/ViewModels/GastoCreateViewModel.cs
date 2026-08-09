using System.ComponentModel.DataAnnotations;

namespace ControlGastos.Web.Models.ViewModels;

public class GastoCreateViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Selecciona una categoría.")]
    public int IdCategoriaGasto { get; init; }
    public int? IdMedioPago { get; init; }
    [Range(typeof(decimal), "0.01", "9999999999", ErrorMessage = "El monto debe ser mayor que cero.")]
    public decimal Monto { get; init; }
    [Required(ErrorMessage = "La fecha es obligatoria.")]
    public DateTime FechaGasto { get; init; }
    [StringLength(150)]
    public string? NombreComercio { get; init; }
    [StringLength(300)]
    public string? Descripcion { get; init; }
    public IFormFile? Comprobante { get; init; }
}
