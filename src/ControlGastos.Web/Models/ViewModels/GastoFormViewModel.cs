namespace ControlGastos.Web.Models.ViewModels;

public interface IGastoFormViewModel
{
    GastoCreateViewModel GastoFormulario { get; }
    IReadOnlyList<CategoriaViewModel> Categorias { get; }
    IReadOnlyList<MedioPagoViewModel> MediosPago { get; }
}

public sealed class GastoFormViewModel<TGasto> : IGastoFormViewModel where TGasto : GastoCreateViewModel
{
    public required TGasto Gasto { get; init; }
    public GastoCreateViewModel GastoFormulario => Gasto;
    public IReadOnlyList<CategoriaViewModel> Categorias { get; init; } = [];
    public IReadOnlyList<MedioPagoViewModel> MediosPago { get; init; } = [];
}

public sealed class MedioPagoViewModel
{
    public int IdMedioPago { get; init; }
    public string Nombre { get; init; } = string.Empty;
}

public sealed class ComprobanteUrlViewModel
{
    public string Url { get; init; } = string.Empty;
    public int ExpiraEnSegundos { get; init; }
}

public sealed class GastoRegistradoViewModel
{
    public long IdGasto { get; init; }
}
