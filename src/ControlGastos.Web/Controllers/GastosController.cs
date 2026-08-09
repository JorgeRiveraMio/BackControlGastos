using ControlGastos.Web.Interfaces;
using ControlGastos.Web.Models;
using ControlGastos.Web.Models.Responses;
using ControlGastos.Web.Models.ViewModels;
using ControlGastos.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace ControlGastos.Web.Controllers;

[SessionAuthorize]
public sealed class GastosController(
    IControlGastosApiClient apiClient,
    ILogger<GastosController> logger) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(int? anio, int? mes, CancellationToken cancellationToken)
    {
        var ahora = FechaLimaHelper.ObtenerAhoraLima();
        var anioSeleccionado = anio is >= 2000 and <= 2100 ? anio.Value : ahora.Year;
        var mesSeleccionado = mes is >= 1 and <= 12 ? mes.Value : ahora.Month;
        var resultado = await apiClient.ObtenerGastosAsync(Token(), anioSeleccionado, mesSeleccionado, cancellationToken);
        if (SesionExpirada(resultado)) return RedirigirALogin();

        if (!resultado.IsSuccess || resultado.Data is null)
        {
            TempData["Error"] = resultado.ErrorMessage ?? "No fue posible obtener los gastos.";
        }

        var gastos = resultado.Data ?? [];
        return View(new GastosIndexViewModel
        {
            Anio = anioSeleccionado,
            Mes = mesSeleccionado,
            Gastos = gastos,
            TotalMes = gastos.Sum(gasto => gasto.Monto)
        });
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var modelo = new GastoCreateViewModel { FechaGasto = FechaLimaHelper.ObtenerAhoraLima() };
        return await VistaFormularioAsync(modelo, cancellationToken);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind(Prefix = "Gasto")] GastoCreateViewModel modelo, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return await VistaFormularioAsync(modelo, cancellationToken);
        }

        var resultado = await apiClient.RegistrarGastoAsync(Token(), Mapear(modelo), cancellationToken);
        if (SesionExpirada(resultado)) return RedirigirALogin();
        if (!resultado.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, resultado.ErrorMessage ?? "No fue posible registrar el gasto.");
            return await VistaFormularioAsync(modelo, cancellationToken);
        }

        if (modelo.Comprobante is not null && modelo.Comprobante.Length > 0)
        {
            await using var contenido = modelo.Comprobante.OpenReadStream();
            var comprobante = await apiClient.SubirComprobanteAsync(
                Token(), resultado.Data!.IdGasto, contenido, modelo.Comprobante.FileName,
                modelo.Comprobante.ContentType, cancellationToken);
            if (SesionExpirada(comprobante)) return RedirigirALogin();
            if (!comprobante.IsSuccess)
            {
                logger.LogWarning("El gasto {IdGasto} fue registrado, pero fallÃ³ la carga del comprobante: {Error}",
                    resultado.Data.IdGasto, comprobante.ErrorMessage);
                TempData["Warning"] = "El gasto fue registrado, pero no se pudo cargar el comprobante.";
                return RedirectToAction(nameof(Index));
            }
        }

        TempData["Success"] = "Gasto registrado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(long id, CancellationToken cancellationToken)
    {
        var resultado = await apiClient.ObtenerGastoAsync(Token(), id, cancellationToken);
        if (SesionExpirada(resultado)) return RedirigirALogin();
        if (resultado.IsNotFound)
        {
            TempData["Error"] = "No se encontrÃ³ el gasto solicitado.";
            return RedirectToAction(nameof(Index));
        }
        if (!resultado.IsSuccess || resultado.Data is null)
        {
            TempData["Error"] = resultado.ErrorMessage ?? "No fue posible obtener el gasto.";
            return RedirectToAction(nameof(Index));
        }

        var gasto = resultado.Data;
        return await VistaFormularioAsync(new GastoEditViewModel
        {
            IdGasto = gasto.IdGasto,
            IdCategoriaGasto = gasto.IdCategoriaGasto,
            IdMedioPago = gasto.IdMedioPago,
            Monto = gasto.Monto,
            FechaGasto = FechaLimaHelper.ConvertirDesdeUtc(gasto.FechaGasto),
            NombreComercio = gasto.NombreComercio,
            Descripcion = gasto.Descripcion
        }, cancellationToken);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(long id, [Bind(Prefix = "Gasto")] GastoEditViewModel modelo, CancellationToken cancellationToken)
    {
        if (id <= 0 || modelo.IdGasto != id)
        {
            return NotFound();
        }
        if (!ModelState.IsValid)
        {
            return await VistaFormularioAsync(modelo, cancellationToken);
        }

        var resultado = await apiClient.ActualizarGastoAsync(Token(), id, Mapear(modelo), cancellationToken);
        if (SesionExpirada(resultado)) return RedirigirALogin();
        if (resultado.IsNotFound)
        {
            TempData["Error"] = "No se encontrÃ³ el gasto solicitado.";
            return RedirectToAction(nameof(Index));
        }
        if (!resultado.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, resultado.ErrorMessage ?? "No fue posible actualizar el gasto.");
            return await VistaFormularioAsync(modelo, cancellationToken);
        }

        TempData["Success"] = "Gasto actualizado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        var resultado = await apiClient.AnularGastoAsync(Token(), id, cancellationToken);
        if (SesionExpirada(resultado)) return RedirigirALogin();
        TempData[resultado.IsSuccess ? "Success" : "Error"] = resultado.IsSuccess
            ? "Gasto anulado correctamente."
            : resultado.ErrorMessage ?? "No fue posible anular el gasto.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Gastos/{id:long}/Comprobante")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubirComprobante(long id, IFormFile? archivo, CancellationToken cancellationToken)
    {
        if (archivo is null || archivo.Length == 0)
        {
            TempData["Error"] = "Seleccione un comprobante no vacÃ­o.";
            return RedirectToAction(nameof(Index));
        }

        await using var contenido = archivo.OpenReadStream();
        var resultado = await apiClient.SubirComprobanteAsync(
            Token(), id, contenido, archivo.FileName, archivo.ContentType, cancellationToken);
        if (SesionExpirada(resultado)) return RedirigirALogin();
        TempData[resultado.IsSuccess ? "Success" : "Error"] = resultado.IsSuccess
            ? "Comprobante cargado correctamente."
            : resultado.ErrorMessage ?? "No fue posible cargar el comprobante.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Gastos/{id:long}/Comprobante")]
    public async Task<IActionResult> ObtenerComprobante(long id, CancellationToken cancellationToken)
    {
        var resultado = await apiClient.ObtenerComprobanteAsync(Token(), id, cancellationToken);
        if (SesionExpirada(resultado)) return RedirigirALogin();
        if (resultado.IsSuccess && !string.IsNullOrWhiteSpace(resultado.Data?.Url))
        {
            return Redirect(resultado.Data.Url);
        }

        TempData["Error"] = resultado.ErrorMessage ?? "No fue posible obtener el comprobante.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Gastos/{id:long}/Comprobante/Url")]
    public async Task<IActionResult> ObtenerUrlComprobante(long id, CancellationToken cancellationToken)
    {
        var resultado = await apiClient.ObtenerComprobanteAsync(Token(), id, cancellationToken);
        if (SesionExpirada(resultado))
        {
            return Unauthorized(new { isOk = false });
        }

        if (!resultado.IsSuccess || string.IsNullOrWhiteSpace(resultado.Data?.Url))
        {
            return NotFound(new { isOk = false });
        }

        return Json(new { isOk = true, url = resultado.Data.Url });
    }

    [HttpPost("Gastos/{id:long}/Comprobante/Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarComprobante(long id, CancellationToken cancellationToken)
    {
        var resultado = await apiClient.EliminarComprobanteAsync(Token(), id, cancellationToken);
        if (SesionExpirada(resultado)) return RedirigirALogin();
        TempData[resultado.IsSuccess ? "Success" : "Error"] = resultado.IsSuccess
            ? "Comprobante eliminado correctamente."
            : resultado.ErrorMessage ?? "No fue posible eliminar el comprobante.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> VistaFormularioAsync<TGasto>(TGasto gasto, CancellationToken cancellationToken)
        where TGasto : GastoCreateViewModel
    {
        var categoriasTask = apiClient.ObtenerCategoriasAsync(Token(), cancellationToken);
        var mediosTask = apiClient.ObtenerMediosPagoAsync(Token(), cancellationToken);
        await Task.WhenAll(categoriasTask, mediosTask);
        var categorias = await categoriasTask;
        var medios = await mediosTask;

        if (SesionExpirada(categorias) || SesionExpirada(medios)) return RedirigirALogin();
        if (!categorias.IsSuccess || !medios.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, categorias.ErrorMessage ?? medios.ErrorMessage ?? "No fue posible cargar los datos del formulario.");
        }

        var modelo = new GastoFormViewModel<TGasto>
        {
            Gasto = gasto,
            Categorias = categorias.Data ?? [],
            MediosPago = medios.Data ?? []
        };
        return gasto is GastoEditViewModel ? View("Edit", modelo) : View("Create", modelo);
    }

    private string Token() => HttpContext.Session.GetString(SessionKeys.AccessToken)!;

    private bool SesionExpirada<T>(ApiClientResult<T> resultado)
    {
        if (!resultado.IsUnauthorized) return false;
        HttpContext.Session.Clear();
        TempData["Error"] = "Tu sesiÃ³n expirÃ³. Inicia sesiÃ³n nuevamente.";
        return true;
    }

    private IActionResult RedirigirALogin() => RedirectToAction("Login", "Account", new { returnUrl = Url.Action(nameof(Index)) });

    private static GastoApiRequest Mapear(GastoCreateViewModel gasto) => new()
    {
        IdCategoriaGasto = gasto.IdCategoriaGasto,
        IdMedioPago = gasto.IdMedioPago,
        Monto = gasto.Monto,
        FechaGasto = FechaLimaHelper.ConvertirALima(gasto.FechaGasto),
        NombreComercio = gasto.NombreComercio,
        Descripcion = gasto.Descripcion
    };
}
