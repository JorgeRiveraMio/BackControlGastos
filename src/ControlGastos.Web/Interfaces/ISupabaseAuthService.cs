using ControlGastos.Web.Models.Responses;

namespace ControlGastos.Web.Interfaces;

public interface ISupabaseAuthService
{
    Task<AuthResult> IniciarSesionAsync(string email, string password, CancellationToken cancellationToken);

    Task<AuthResult> RegistrarAsync(string email, string password, CancellationToken cancellationToken);
}
