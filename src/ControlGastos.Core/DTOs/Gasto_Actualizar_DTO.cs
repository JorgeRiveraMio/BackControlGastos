namespace ControlGastos.Core.DTOs;

public sealed class Gasto_Actualizar_DTO
{
    public int IdCategoriaGasto { get; init; }

    public int? IdMedioPago { get; init; }

    public decimal Monto { get; init; }

    public DateTimeOffset FechaGasto { get; init; }

    public string? NombreComercio { get; init; }

    public string? Descripcion { get; init; }
}
