namespace ControlGastos.Api.Services;

public interface ICategorizadorGastoTelegramService
{
    string SugerirCategoria(string descripcion);
}
