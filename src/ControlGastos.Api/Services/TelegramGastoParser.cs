using System.Globalization;
using System.Text.RegularExpressions;

namespace ControlGastos.Api.Services;

public sealed partial class TelegramGastoParser : ITelegramGastoParser
{
    private const int LongitudMaximaDescripcion = 250;

    public TelegramGastoParseResult Parse(string texto)
    {
        var coincidencia = GastoRegex().Match(texto.Trim());
        if (!coincidencia.Success)
        {
            return Invalido("Escribe un monto seguido de una descripción.");
        }

        var montoTexto = coincidencia.Groups["monto"].Value.Replace(',', '.');
        if (!decimal.TryParse(montoTexto, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var monto) || monto <= 0)
        {
            return Invalido("El monto debe ser mayor que cero.");
        }

        if (decimal.Round(monto, 2) != monto || monto > 9_999_999_999.99m)
        {
            return Invalido("El monto debe tener como máximo dos decimales y estar dentro de un rango válido.");
        }

        var descripcion = coincidencia.Groups["descripcion"].Value.Trim();
        if (descripcion.Length == 0 || descripcion.Length > LongitudMaximaDescripcion)
        {
            return Invalido($"La descripción debe tener entre 1 y {LongitudMaximaDescripcion} caracteres.");
        }

        return new TelegramGastoParseResult(true, monto, descripcion, null);
    }

    private static TelegramGastoParseResult Invalido(string error) => new(false, 0, null, error);

    [GeneratedRegex(@"^(?<monto>\d+(?:[.,]\d+)?)\s+(?<descripcion>.+)$", RegexOptions.CultureInvariant)]
    private static partial Regex GastoRegex();
}
