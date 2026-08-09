namespace ControlGastos.Core.Entities;

public sealed class Alerta
{
    public long IdAlerta { get; set; }
    public Guid IdUsuario { get; set; }
    public required string CodigoTipoAlerta { get; set; }
    public required string DescripcionAlerta { get; set; }
    public decimal? PorcentajeUmbral { get; set; }
    public decimal? MontoPresupuesto { get; set; }
    public decimal? MontoGastado { get; set; }
    public int AnioPeriodo { get; set; }
    public int MesPeriodo { get; set; }
    public bool EstadoLeida { get; set; }
    public DateTimeOffset FechaRegistro { get; set; }
}
