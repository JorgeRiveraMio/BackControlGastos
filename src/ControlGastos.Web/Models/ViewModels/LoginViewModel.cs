using System.ComponentModel.DataAnnotations;

namespace ControlGastos.Web.Models.ViewModels;

public sealed class LoginViewModel
{
    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "Ingresa un correo válido.")]
    public string Email { get; init; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [DataType(DataType.Password)]
    public string Password { get; init; } = string.Empty;

    public string? ReturnUrl { get; init; }
}
