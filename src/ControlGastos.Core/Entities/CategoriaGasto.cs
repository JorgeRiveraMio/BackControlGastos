namespace ControlGastos.Core.Entities;

public sealed class CategoriaGasto
{
    public int IdCategoriaGasto { get; set; }

    public required string Nombre { get; set; }

    public bool EstaActiva { get; set; }

    public DateTimeOffset FechaRegistro { get; set; }
}
