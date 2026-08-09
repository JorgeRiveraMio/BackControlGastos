using ControlGastos.Core.Entities;
namespace ControlGastos.Core.Interfaces;
public interface ITelegramNotificadorService { Task NotificarAlertaAsync(Guid idUsuario, Alerta alerta, CancellationToken ct); }
