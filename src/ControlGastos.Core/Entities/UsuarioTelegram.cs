namespace ControlGastos.Core.Entities;
public sealed class UsuarioTelegram { public Guid IdUsuario { get; set; } public long IdTelegram { get; set; } public long IdChatTelegram { get; set; } public string? NombreUsuarioTelegram { get; set; } public bool EstaVinculado { get; set; } public DateTimeOffset FechaVinculacion { get; set; } public DateTimeOffset? FechaActualizacion { get; set; } }
