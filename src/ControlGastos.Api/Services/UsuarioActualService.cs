using System.Security.Claims;
using ControlGastos.Core.Interfaces;

namespace ControlGastos.Api.Services;

public sealed class UsuarioActualService(IHttpContextAccessor httpContextAccessor) : IUsuarioActualService
{
    public Guid ObtenerIdUsuario()
    {
        var usuario = httpContextAccessor.HttpContext?.User;
        var subject = usuario?.FindFirstValue("sub")
            ?? usuario?.FindFirstValue(ClaimTypes.NameIdentifier);

        if (usuario?.Identity?.IsAuthenticated != true || !Guid.TryParse(subject, out var idUsuario))
        {
            throw new UnauthorizedAccessException("No se pudo determinar el usuario autenticado.");
        }

        return idUsuario;
    }
}
