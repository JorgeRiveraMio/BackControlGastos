using System.ComponentModel.DataAnnotations;

namespace ControlGastos.Web.Models.ViewModels;

public sealed class PerfilFinancieroViewModel
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(120)]
    public string NombreUsuario { get; init; } = string.Empty;
    [Range(typeof(decimal), "0", "9999999999", ErrorMessage = "El ingreso no puede ser negativo.")]
    public decimal? IngresoMensual { get; init; }
    [Range(typeof(decimal), "0", "9999999999", ErrorMessage = "El presupuesto no puede ser negativo.")]
    public decimal? PresupuestoMensual { get; init; }
    [Range(typeof(decimal), "0.01", "100", ErrorMessage = "El porcentaje debe estar entre 0 y 100.")]
    public decimal PorcentajeAlerta { get; init; } = 80;
}

public sealed class PerfilFinancieroApiModel
{
    public string NombreUsuario { get; init; } = string.Empty;
    public string CodigoMoneda { get; init; } = "PEN";
    public string ZonaHoraria { get; init; } = "America/Lima";
    public decimal? IngresoMensual { get; init; }
    public decimal? PresupuestoMensual { get; init; }
    public decimal PorcentajeAlerta { get; init; }
}

public sealed class PerfilFinancieroActualizarApiModel
{
    public string NombreUsuario { get; init; } = string.Empty;
    public decimal? IngresoMensual { get; init; }
    public decimal? PresupuestoMensual { get; init; }
    public decimal PorcentajeAlerta { get; init; }
}
