using System.Globalization;
using System.Text;

namespace ControlGastos.Api.Services;

public sealed class CategorizadorGastoTelegramService : ICategorizadorGastoTelegramService
{
    private static readonly IReadOnlyDictionary<string, string[]> Reglas = new Dictionary<string, string[]>
    {
        ["Alimentación"] = ["almuerzo", "desayuno", "cena", "comida", "restaurante", "pollo", "pizza", "hamburguesa", "café", "cafe"],
        ["Transporte"] = ["taxi", "uber", "cabify", "bus", "pasaje", "metropolitano", "gasolina", "combustible"],
        ["Entretenimiento"] = ["cine", "netflix", "spotify", "juego", "discoteca"],
        ["Servicios"] = ["internet", "luz", "agua", "telefono", "teléfono", "recarga"],
        ["Salud"] = ["farmacia", "medicina", "doctor", "clínica", "clinica"],
        ["Educación"] = ["curso", "libro", "universidad", "udemy"],
        ["Compras"] = ["tambo", "oxxo", "ropa", "zapatillas"]
    };

    public string SugerirCategoria(string descripcion)
    {
        var normalizada = Normalizar(descripcion);
        foreach (var (categoria, palabras) in Reglas)
        {
            if (palabras.Any(palabra => normalizada.Contains(Normalizar(palabra), StringComparison.Ordinal)))
            {
                return categoria;
            }
        }

        return "Otros";
    }

    public static string Normalizar(string valor)
    {
        var descompuesto = valor.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(descompuesto.Length);
        foreach (var caracter in descompuesto)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(caracter) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(char.ToLowerInvariant(caracter));
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
