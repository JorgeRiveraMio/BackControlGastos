using ControlGastos.Core.Entities;
namespace ControlGastos.Core.Interfaces;
public interface IUsuarioTelegramRepository { Task<UsuarioTelegram?> ObtenerAsync(Guid idUsuario, CancellationToken ct); Task VincularAsync(Guid idUsuario, long telegramUserId, long chatId, string? username, CancellationToken ct); Task DesvincularAsync(Guid idUsuario, CancellationToken ct); }
