namespace ControlGastos.Core.Interfaces;
public interface ITelegramBotClientService
{
    Task EnviarMensajeAsync(long chatId, string mensaje, CancellationToken ct);
    Task EnviarMensajeConBotonesAsync(long chatId, string mensaje, long idGastoPendiente, CancellationToken ct);
    Task EditarMensajeAsync(long chatId, long messageId, string mensaje, object replyMarkup, CancellationToken ct);
    Task EnviarMensajeConMarkupAsync(long chatId, string mensaje, object replyMarkup, CancellationToken ct);
    Task ResponderCallbackAsync(string callbackQueryId, CancellationToken ct);
}
