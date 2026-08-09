namespace ControlGastos.Core.DTOs;

public sealed class Alerta_Listar_DTO
{
    public long IdAlerta { get; init; }
    public string CodigoTipoAlerta { get; init; } = string.Empty;
    public string Mensaje { get; init; } = string.Empty;
    public decimal? PorcentajeUmbral { get; init; }
    public decimal? MontoPresupuesto { get; init; }
    public decimal? MontoGastado { get; init; }
    public int Anio { get; init; }
    public int Mes { get; init; }
    public bool Leida { get; init; }
    public DateTimeOffset FechaRegistro { get; init; }
}
