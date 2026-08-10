using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using ControlGastos.Api.Services;
using ControlGastos.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ControlGastos.Api.Controllers;

[ApiController]
[Route("api/telegram/webhook")]
public sealed class TelegramWebhookController(
    IVinculacionTelegramRepository tokens,
    IUsuarioTelegramRepository usuarios,
    ITelegramBotClientService bot,
    IOptions<TelegramOptions> options,
    ILogger<TelegramWebhookController> logger) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Recibir([FromBody] TelegramUpdate update, CancellationToken ct)
    {
        var configurado = options.Value.WebhookSecret;
        var recibido = Request.Headers["X-Telegram-Bot-Api-Secret-Token"].ToString();

        if (string.IsNullOrWhiteSpace(configurado) ||
            !CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(configurado),
                Encoding.UTF8.GetBytes(recibido)))
        {
            return Unauthorized();
        }

        var texto = update.Message?.Text;
        if (string.IsNullOrWhiteSpace(texto) || !texto.StartsWith("/start ", StringComparison.Ordinal))
        {
            return Ok();
        }

        var token = texto[7..].Trim();
        var vinculacion = await tokens.ObtenerValidaAsync(token, ct);
        var chat = update.Message?.Chat;
        var from = update.Message?.From;

        if (vinculacion is null || chat is null || from is null)
        {
            if (chat is not null)
            {
                await EnviarMensajeSinReintentoAsync(chat.Id, "El enlace de vinculación no es válido o expiró.", ct);
            }

            return Ok();
        }

        await usuarios.VincularAsync(vinculacion.IdUsuario, from.Id, chat.Id, from.Username, ct);
        await tokens.MarcarUsadaAsync(vinculacion.IdVinculacion, ct);

        await EnviarMensajeSinReintentoAsync(
            chat.Id,
            "Telegram quedó vinculado correctamente con tu cuenta de ControlGastos.",
            ct);

        return Ok();
    }

    private async Task EnviarMensajeSinReintentoAsync(long chatId, string mensaje, CancellationToken ct)
    {
        try
        {
            await bot.EnviarMensajeAsync(chatId, mensaje, ct);
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                "No se pudo enviar el mensaje de respuesta del webhook para el chat {ChatId}. Tipo de error: {ExceptionType}.",
                chatId,
                exception.GetType().Name);
        }
    }
}

public sealed class TelegramUpdate
{
    [JsonPropertyName("update_id")]
    public long UpdateId { get; init; }

    public TelegramMessage? Message { get; init; }
}

public sealed class TelegramMessage
{
    public string? Text { get; init; }
    public TelegramChat? Chat { get; init; }
    public TelegramUser? From { get; init; }
}

public sealed class TelegramChat
{
    public long Id { get; init; }
}

public sealed class TelegramUser
{
    public long Id { get; init; }
    public string? Username { get; init; }

    [JsonPropertyName("first_name")]
    public string? FirstName { get; init; }
}
