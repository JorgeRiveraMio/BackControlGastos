namespace ControlGastos.Api.Services;

public interface ITelegramGastoService
{
    Task<TelegramGastoPropuestaResult> CrearPropuestaAsync(long chatId, string texto, CancellationToken ct);
    Task<TelegramGastoCallbackResult> ConfirmarAsync(long idGastoPendiente, long chatId, long telegramUserId, CancellationToken ct);
    Task<TelegramGastoCallbackResult> CancelarAsync(long idGastoPendiente, long chatId, long telegramUserId, CancellationToken ct);
}

public sealed record TelegramGastoPropuestaResult(
    TelegramGastoPropuestaEstado Estado,
    long? IdGastoPendiente = null,
    decimal? Monto = null,
    string? Descripcion = null,
    string? Categoria = null);

public enum TelegramGastoPropuestaEstado { Creada, NoVinculado, FormatoInvalido, CategoriaNoDisponible }

public sealed record TelegramGastoCallbackResult(
    TelegramGastoCallbackEstado Estado,
    decimal? Monto = null,
    string? Categoria = null,
    Guid? IdUsuario = null,
    DateTimeOffset? FechaGasto = null);

public enum TelegramGastoCallbackEstado { Confirmado, YaConfirmado, Cancelado, YaCancelado, Expirado, NoEncontrado }
