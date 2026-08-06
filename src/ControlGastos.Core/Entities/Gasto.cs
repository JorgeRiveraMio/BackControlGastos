namespace ControlGastos.Core.Entities;

public sealed class Gasto
{
    public long IdGasto { get; set; }

    public Guid IdUsuario { get; set; }

    public int IdCategoriaGasto { get; set; }

    public int? IdMedioPago { get; set; }

    public required string CodigoEstado { get; set; }

    public decimal Monto { get; set; }

    public DateTimeOffset FechaGasto { get; set; }

    public required string NombreComercio { get; set; }

    public required string Descripcion { get; set; }

    public required string CodigoOrigen { get; set; }

    public required string RutaComprobante { get; set; }

    public required string NombreComprobante { get; set; }

    public DateTimeOffset FechaRegistro { get; set; }

    public DateTimeOffset FechaActualizacion { get; set; }
}
