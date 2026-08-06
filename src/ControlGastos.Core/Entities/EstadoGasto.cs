namespace ControlGastos.Core.Entities;

public sealed class EstadoGasto
{
    public required string CodigoEstado { get; set; }

    public required string Nombre { get; set; }

    public bool EstaActivo { get; set; }
}
