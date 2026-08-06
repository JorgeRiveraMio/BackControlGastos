namespace ControlGastos.Core.Entities;

public sealed class MedioPago
{
    public int IdMedioPago { get; set; }

    public required string Nombre { get; set; }

    public bool EstaActivo { get; set; }

    public DateTimeOffset FechaRegistro { get; set; }
}
