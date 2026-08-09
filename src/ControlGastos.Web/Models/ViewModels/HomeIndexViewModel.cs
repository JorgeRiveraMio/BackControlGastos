namespace ControlGastos.Web.Models.ViewModels;

public sealed class HomeIndexViewModel
{
    public IReadOnlyList<CategoriaViewModel> Categorias { get; init; } = [];
}

public sealed class CategoriaViewModel
{
    public int IdCategoriaGasto { get; init; }

    public string Nombre { get; init; } = string.Empty;
}
