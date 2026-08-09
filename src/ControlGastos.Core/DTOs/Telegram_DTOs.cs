namespace ControlGastos.Core.DTOs;
public sealed class TelegramEstado_DTO { public bool Vinculado { get; init; } public string? Username { get; init; } }
public sealed class TelegramVinculacion_DTO { public string BotUsername { get; init; } = string.Empty; public string Token { get; init; } = string.Empty; public int ExpiraEnSegundos { get; init; } }
