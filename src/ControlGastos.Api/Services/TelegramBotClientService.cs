using System.Net.Http.Json;
using ControlGastos.Core.Interfaces;
using Microsoft.Extensions.Options;

namespace ControlGastos.Api.Services;

public sealed class TelegramBotClientService(
    HttpClient client,
    IOptions<TelegramOptions> options,
    ILogger<TelegramBotClientService> logger) : ITelegramBotClientService
{
    public async Task EnviarMensajeAsync(long chatId, string mensaje, CancellationToken ct)
    {
        var botToken = options.Value.BotToken;
        if (string.IsNullOrWhiteSpace(botToken))
        {
            logger.LogWarning("No se envió un mensaje de Telegram porque Telegram:BotToken no está configurado.");
            return;
        }

        // El prefijo '/' fuerza una URI relativa a BaseAddress; sin él, el ':' del token se interpreta como esquema URI.
        var requestUri = new Uri($"/bot{botToken}/sendMessage", UriKind.Relative);
        var payload = new { chat_id = chatId, text = mensaje };

        try
        {
            using var response = await client.PostAsJsonAsync(requestUri, payload, ct);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Telegram rechazó el envío de un mensaje para el chat {ChatId}. Código HTTP: {StatusCode}.",
                    chatId,
                    (int)response.StatusCode);
            }

            response.EnsureSuccessStatusCode();
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            logger.LogError(
                "No se pudo enviar un mensaje de Telegram para el chat {ChatId}. Tipo de error: {ExceptionType}.",
                chatId,
                exception.GetType().Name);
            throw;
        }
    }
}
