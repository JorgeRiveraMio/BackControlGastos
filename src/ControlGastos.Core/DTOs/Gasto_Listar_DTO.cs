namespace ControlGastos.Core.DTOs;

public sealed class Gasto_Listar_DTO
{
    public long IdGasto { get; init; }

    public Guid IdUsuario { get; init; }

    public int IdCategoriaGasto { get; init; }

    public required string NombreCategoria { get; init; }

    public int? IdMedioPago { get; init; }

    public string? NombreMedioPago { get; init; }

    public decimal Monto { get; init; }

    public DateTimeOffset FechaGasto { get; init; }

    public string? NombreComercio { get; init; }

    public string? Descripcion { get; init; }

    public required string CodEstado { get; init; }

    public required string CodOrigen { get; init; }
}
