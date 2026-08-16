namespace ControlGastos.Api.Services;

public interface ITelegramGastoParser
{
    TelegramGastoParseResult Parse(string texto);
}

public sealed record TelegramGastoParseResult(bool EsValido, decimal Monto, string? Descripcion, string? Error);
