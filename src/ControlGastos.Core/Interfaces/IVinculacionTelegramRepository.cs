using ControlGastos.Core.Entities;
namespace ControlGastos.Core.Interfaces;
public interface IVinculacionTelegramRepository { Task CrearAsync(VinculacionTelegram vinculacion, CancellationToken ct); Task<VinculacionTelegram?> ObtenerValidaAsync(string token, CancellationToken ct); Task MarcarUsadaAsync(long idVinculacion, CancellationToken ct); }
