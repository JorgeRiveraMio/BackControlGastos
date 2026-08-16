namespace ControlGastos.Core.Entities;

public sealed class GastoPendienteTelegram
{
    public long IdGastoPendiente { get; set; }
    public Guid IdUsuario { get; set; }
    public long IdChatTelegram { get; set; }
    public decimal Monto { get; set; }
    public required string Descripcion { get; set; }
    public int IdCategoriaGasto { get; set; }
    public DateTimeOffset FechaGasto { get; set; }
    public required string CodEstado { get; set; }
    public DateTimeOffset FechaExpiracion { get; set; }
    public DateTimeOffset FechaRegistro { get; set; }
    public DateTimeOffset? FechaActualizacion { get; set; }
}
