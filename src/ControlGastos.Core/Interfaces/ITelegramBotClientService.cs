namespace ControlGastos.Core.Interfaces;
public interface ITelegramBotClientService { Task EnviarMensajeAsync(long chatId, string mensaje, CancellationToken ct); }
