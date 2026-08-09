using ControlGastos.Web.Interfaces;
using ControlGastos.Web.Models;
using ControlGastos.Web.Models.Responses;
using ControlGastos.Web.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace ControlGastos.Web.Controllers;

public sealed class AccountController(ISupabaseAuthService supabaseAuthService) : Controller
{
    [HttpGet]
    public IActionResult Login(string? returnUrl = null) => View(new LoginViewModel { ReturnUrl = returnUrl });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var resultado = await supabaseAuthService.IniciarSesionAsync(model.Email, model.Password, cancellationToken);
        if (!resultado.IsSuccess || string.IsNullOrWhiteSpace(resultado.AccessToken))
        {
            ModelState.AddModelError(string.Empty, "Correo o contraseña incorrectos.");
            return View(model);
        }

        GuardarSesion(resultado);
        return LocalRedirect(Url.IsLocalUrl(model.ReturnUrl) ? model.ReturnUrl : Url.Action("Index", "Dashboard")!);
    }

    [HttpGet]
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var resultado = await supabaseAuthService.RegistrarAsync(model.Email, model.Password, cancellationToken);
        if (!resultado.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, "No fue posible crear la cuenta. Inténtalo nuevamente.");
            return View(model);
        }

        if (resultado.RequiresEmailConfirmation || string.IsNullOrWhiteSpace(resultado.AccessToken))
        {
            TempData["SuccessMessage"] = "Revisa tu correo para confirmar tu cuenta.";
            return RedirectToAction(nameof(Login));
        }

        GuardarSesion(resultado);
        return RedirectToAction("Index", "Dashboard");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction(nameof(Login));
    }

    private void GuardarSesion(AuthResult resultado)
    {
        HttpContext.Session.SetString(SessionKeys.AccessToken, resultado.AccessToken!);
        HttpContext.Session.SetString(SessionKeys.RefreshToken, resultado.RefreshToken ?? string.Empty);
        HttpContext.Session.SetString(
            SessionKeys.AccessTokenExpiresAt,
            DateTimeOffset.UtcNow.AddSeconds(resultado.ExpiresIn).ToString("O"));
    }
}
