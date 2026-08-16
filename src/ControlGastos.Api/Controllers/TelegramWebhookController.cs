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
    ITelegramGastoService gastosTelegram,
    IEvaluadorPresupuestoService evaluadorPresupuestoService,
    IOptions<TelegramOptions> options,
    ILogger<TelegramWebhookController> logger) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Recibir([FromBody] TelegramUpdate update, CancellationToken ct)
    {
        if (!EsWebhookValido())
        {
            return Unauthorized();
        }

        if (update.CallbackQuery is not null)
        {
            await ProcesarCallbackAsync(update.CallbackQuery, ct);
            return Ok();
        }

        if (update.Message is not null)
        {
            await ProcesarMensajeAsync(update.Message, ct);
        }

        return Ok();
    }

    private bool EsWebhookValido()
    {
        var configurado = options.Value.WebhookSecret;
        var recibido = Request.Headers["X-Telegram-Bot-Api-Secret-Token"].ToString();
        return !string.IsNullOrWhiteSpace(configurado) &&
               CryptographicOperations.FixedTimeEquals(
                   Encoding.UTF8.GetBytes(configurado),
                   Encoding.UTF8.GetBytes(recibido));
    }

    private async Task ProcesarMensajeAsync(TelegramMessage mensaje, CancellationToken ct)
    {
        var texto = mensaje.Text?.Trim();
        var chat = mensaje.Chat;
        if (string.IsNullOrWhiteSpace(texto) || chat is null)
        {
            return;
        }

        if (texto.StartsWith("/start ", StringComparison.Ordinal))
        {
            await ProcesarInicioAsync(texto[7..].Trim(), mensaje, ct);
            return;
        }

        var textoGasto = texto.StartsWith("/gasto ", StringComparison.OrdinalIgnoreCase)
            ? texto[7..].Trim()
            : texto;
        var propuesta = await gastosTelegram.CrearPropuestaAsync(chat.Id, textoGasto, ct);

        switch (propuesta.Estado)
        {
            case TelegramGastoPropuestaEstado.Creada:
                await EnviarSinReintentoAsync(
                    () => bot.EnviarMensajeConBotonesAsync(
                        chat.Id,
                        $"Gasto detectado 🧾\n\nMonto: S/ {propuesta.Monto:N2}\nDescripción: {propuesta.Descripcion}\nCategoría: {propuesta.Categoria}\n\n¿Deseas registrarlo?",
                        propuesta.IdGastoPendiente!.Value,
                        ct),
                    chat.Id);
                break;
            case TelegramGastoPropuestaEstado.NoVinculado:
                await EnviarSinReintentoAsync(
                    () => bot.EnviarMensajeAsync(chat.Id, "Primero vincula tu cuenta de Telegram desde tu perfil de ControlGastos.", ct),
                    chat.Id);
                break;
            case TelegramGastoPropuestaEstado.FormatoInvalido:
                await EnviarSinReintentoAsync(
                    () => bot.EnviarMensajeAsync(chat.Id, "No pude interpretar el gasto.\n\nPrueba escribiendo:\n18.50 almuerzo\no\n/gasto 18.50 almuerzo", ct),
                    chat.Id);
                break;
            case TelegramGastoPropuestaEstado.CategoriaNoDisponible:
                await EnviarSinReintentoAsync(
                    () => bot.EnviarMensajeAsync(chat.Id, "No encontré una categoría activa para ese gasto. Revisa tus categorías en ControlGastos.", ct),
                    chat.Id);
                break;
        }
    }

    private async Task ProcesarInicioAsync(string token, TelegramMessage mensaje, CancellationToken ct)
    {
        var chat = mensaje.Chat;
        var from = mensaje.From;
        var vinculacion = await tokens.ObtenerValidaAsync(token, ct);
        if (vinculacion is null || chat is null || from is null)
        {
            if (chat is not null)
            {
                await EnviarSinReintentoAsync(
                    () => bot.EnviarMensajeAsync(chat.Id, "El enlace de vinculación no es válido o expiró.", ct),
                    chat.Id);
            }

            return;
        }

        await usuarios.VincularAsync(vinculacion.IdUsuario, from.Id, chat.Id, from.Username, ct);
        await tokens.MarcarUsadaAsync(vinculacion.IdVinculacion, ct);
        await EnviarSinReintentoAsync(
            () => bot.EnviarMensajeAsync(chat.Id, "Telegram quedó vinculado correctamente con tu cuenta de ControlGastos.", ct),
            chat.Id);
    }

    private async Task ProcesarCallbackAsync(TelegramCallbackQuery callback, CancellationToken ct)
    {
        var chat = callback.Message?.Chat;
        if (chat is null || !TryObtenerAccion(callback.Data, out var confirmar, out var idGastoPendiente))
        {
            await EnviarCallbackSinReintentoAsync(callback.Id, ct);
            return;
        }

        var resultado = confirmar
            ? await gastosTelegram.ConfirmarAsync(idGastoPendiente, chat.Id, callback.From.Id, ct)
            : await gastosTelegram.CancelarAsync(idGastoPendiente, chat.Id, callback.From.Id, ct);
        await EnviarCallbackSinReintentoAsync(callback.Id, ct);

        if (resultado.Estado == TelegramGastoCallbackEstado.Confirmado && resultado.IdUsuario.HasValue && resultado.FechaGasto.HasValue)
        {
            try
            {
                await evaluadorPresupuestoService.EvaluarAsync(resultado.IdUsuario.Value, resultado.FechaGasto.Value, ct);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "No se pudo evaluar el presupuesto después de registrar un gasto por Telegram.");
            }
        }

        var respuesta = resultado.Estado switch
        {
            TelegramGastoCallbackEstado.Confirmado => $"Gasto registrado correctamente ✅\nS/ {resultado.Monto:N2} · {resultado.Categoria}",
            TelegramGastoCallbackEstado.YaConfirmado => "Este gasto ya fue registrado correctamente.",
            TelegramGastoCallbackEstado.Cancelado or TelegramGastoCallbackEstado.YaCancelado => "Gasto cancelado.",
            TelegramGastoCallbackEstado.Expirado => "Esta solicitud expiró. Envía el gasto nuevamente.",
            _ => "No encontré una solicitud de gasto válida para este chat."
        };

        await EnviarSinReintentoAsync(() => bot.EnviarMensajeAsync(chat.Id, respuesta, ct), chat.Id);
    }

    private static bool TryObtenerAccion(string? data, out bool confirmar, out long idGastoPendiente)
    {
        confirmar = false;
        idGastoPendiente = 0;
        if (string.IsNullOrWhiteSpace(data)) return false;

        var partes = data.Split(':', 2, StringSplitOptions.TrimEntries);
        if (partes.Length != 2 || !long.TryParse(partes[1], out idGastoPendiente) || idGastoPendiente <= 0) return false;

        confirmar = partes[0] == "gasto_ok";
        return confirmar || partes[0] == "gasto_cancel";
    }

    private async Task EnviarCallbackSinReintentoAsync(string callbackId, CancellationToken ct) =>
        await EnviarSinReintentoAsync(() => bot.ResponderCallbackAsync(callbackId, ct), null);

    private async Task EnviarSinReintentoAsync(Func<Task> enviar, long? chatId)
    {
        try
        {
            await enviar();
        }
        catch (Exception exception)
        {
            logger.LogWarning("No se pudo enviar una respuesta de Telegram para el chat {ChatId}. Tipo de error: {ExceptionType}.", chatId, exception.GetType().Name);
        }
    }
}

public sealed class TelegramUpdate
{
    [JsonPropertyName("update_id")]
    public long UpdateId { get; init; }

    [JsonPropertyName("message")]
    public TelegramMessage? Message { get; init; }

    [JsonPropertyName("callback_query")]
    public TelegramCallbackQuery? CallbackQuery { get; init; }
}

public sealed class TelegramCallbackQuery
{
    public string Id { get; init; } = string.Empty;
    public TelegramUser From { get; init; } = new();
    public TelegramMessage? Message { get; init; }
    public string? Data { get; init; }
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
