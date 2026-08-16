namespace ControlGastos.Core.Interfaces;
public interface ITelegramBotClientService
{
    Task EnviarMensajeAsync(long chatId, string mensaje, CancellationToken ct);
    Task EnviarMensajeConBotonesAsync(long chatId, string mensaje, long idGastoPendiente, CancellationToken ct);
    Task ResponderCallbackAsync(string callbackQueryId, CancellationToken ct);
}
