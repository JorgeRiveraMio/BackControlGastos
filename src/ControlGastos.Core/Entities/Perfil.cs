namespace ControlGastos.Core.Entities;

public sealed class Perfil
{
    public Guid IdUsuario { get; set; }

    public required string NombreUsuario { get; set; }

    public required string CodigoMoneda { get; set; }

    public required string ZonaHoraria { get; set; }

    public decimal? IngresoMensual { get; set; }

    public decimal? PresupuestoMensual { get; set; }

    public decimal PorcentajeAlerta { get; set; }

    public bool EstadoPerfil { get; set; }

    public DateTimeOffset FechaRegistro { get; set; }

    public DateTimeOffset? FechaActualizacion { get; set; }
}
