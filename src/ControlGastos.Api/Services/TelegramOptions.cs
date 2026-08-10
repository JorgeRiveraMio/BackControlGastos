namespace ControlGastos.Api.Services;

public sealed class TelegramOptions
{
    public const string SectionName = "Telegram";

    public string BotToken { get; init; } = string.Empty;
    public string BotUsername { get; init; } = string.Empty;
    public string WebhookSecret { get; init; } = string.Empty;
}
