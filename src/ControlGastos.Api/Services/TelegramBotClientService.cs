using System.Net.Http.Json;
using ControlGastos.Core.Interfaces;
using Microsoft.Extensions.Options;

namespace ControlGastos.Api.Services;

public sealed class TelegramBotClientService(
    HttpClient client,
    IOptions<TelegramOptions> options,
    ILogger<TelegramBotClientService> logger) : ITelegramBotClientService
{
    public Task EnviarMensajeAsync(long chatId, string mensaje, CancellationToken ct) =>
        EnviarAsync("sendMessage", new { chat_id = chatId, text = mensaje }, chatId, ct);

    public Task EnviarMensajeConBotonesAsync(long chatId, string mensaje, long idGastoPendiente, CancellationToken ct) =>
        EnviarAsync(
            "sendMessage",
            new
            {
                chat_id = chatId,
                text = mensaje,
                reply_markup = new
                {
                    inline_keyboard = new[]
                    {
                        new[]
                        {
                            new { text = "✅ Confirmar", callback_data = $"gasto_ok:{idGastoPendiente}" },
                            new { text = "🏷 Cambiar categoría", callback_data = $"gasto_catmenu:{idGastoPendiente}" }
                        },
                        new[]
                        {
                            new { text = "💳 Medio de pago", callback_data = $"gasto_pagomenu:{idGastoPendiente}" },
                            new { text = "❌ Cancelar", callback_data = $"gasto_cancel:{idGastoPendiente}" }
                        }
                    }
                }
            },
            chatId,
            ct);

    public Task EnviarMensajeConMarkupAsync(long chatId, string mensaje, object replyMarkup, CancellationToken ct) =>
        EnviarAsync("sendMessage", new { chat_id = chatId, text = mensaje, reply_markup = replyMarkup }, chatId, ct);

    public Task EditarMensajeAsync(long chatId, long messageId, string mensaje, object replyMarkup, CancellationToken ct) =>
        EnviarAsync("editMessageText", new { chat_id = chatId, message_id = messageId, text = mensaje, reply_markup = replyMarkup }, chatId, ct);

    public Task ResponderCallbackAsync(string callbackQueryId, CancellationToken ct) =>
        EnviarAsync("answerCallbackQuery", new { callback_query_id = callbackQueryId }, null, ct);

    private async Task EnviarAsync(string metodo, object payload, long? chatId, CancellationToken ct)
    {
        var botToken = options.Value.BotToken;
        if (string.IsNullOrWhiteSpace(botToken))
        {
            logger.LogWarning("No se envió un mensaje de Telegram porque Telegram:BotToken no está configurado.");
            return;
        }

        var requestUri = new Uri($"/bot{botToken}/{metodo}", UriKind.Relative);
        try
        {
            using var response = await client.PostAsJsonAsync(requestUri, payload, ct);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Telegram rechazó una solicitud para el chat {ChatId}. Código HTTP: {StatusCode}.",
                    chatId,
                    (int)response.StatusCode);
            }

            response.EnsureSuccessStatusCode();
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            logger.LogError(
                "No se pudo completar una solicitud a Telegram para el chat {ChatId}. Tipo de error: {ExceptionType}.",
                chatId,
                exception.GetType().Name);
            throw;
        }
    }
}
