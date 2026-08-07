namespace ControlGastos.Core.Entities;

public sealed class Gasto
{
    public long IdGasto { get; set; }

    public Guid IdUsuario { get; set; }

    public int IdCategoriaGasto { get; set; }

    public int? IdMedioPago { get; set; }

    public required string CodEstado { get; set; }

    public decimal Monto { get; set; }

    public DateTimeOffset FechaGasto { get; set; }

    public string? NombreComercio { get; set; }

    public string? Descripcion { get; set; }

    public required string CodOrigen { get; set; }

    public string? RutaComprobante { get; set; }

    public string? NombreComprobante { get; set; }

    public DateTimeOffset FechaRegistro { get; set; }

    public DateTimeOffset? FechaActualizacion { get; set; }
}
