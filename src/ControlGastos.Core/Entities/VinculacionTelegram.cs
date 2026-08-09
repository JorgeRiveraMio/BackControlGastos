namespace ControlGastos.Core.Entities;
public sealed class VinculacionTelegram { public long IdVinculacion { get; set; } public Guid IdUsuario { get; set; } public required string CodigoToken { get; set; } public DateTimeOffset FechaExpiracion { get; set; } public bool EstaUsado { get; set; } public DateTimeOffset FechaRegistro { get; set; } }
