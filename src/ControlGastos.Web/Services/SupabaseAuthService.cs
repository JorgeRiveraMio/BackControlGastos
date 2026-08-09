using System.Net.Http.Json;
using System.Text.Json;
using ControlGastos.Web.Interfaces;
using ControlGastos.Web.Models;
using ControlGastos.Web.Models.Responses;
using Microsoft.Extensions.Options;

namespace ControlGastos.Web.Services;

public sealed class SupabaseAuthService(
    HttpClient httpClient,
    IOptions<SupabaseAuthOptions> options,
    ILogger<SupabaseAuthService> logger) : ISupabaseAuthService
{
    private readonly SupabaseAuthOptions _options = options.Value;

    public Task<AuthResult> IniciarSesionAsync(string email, string password, CancellationToken cancellationToken) =>
        EnviarSolicitudAsync("auth/v1/token?grant_type=password", email, password, false, cancellationToken);

    public Task<AuthResult> RegistrarAsync(string email, string password, CancellationToken cancellationToken) =>
        EnviarSolicitudAsync("auth/v1/signup", email, password, true, cancellationToken);

    private async Task<AuthResult> EnviarSolicitudAsync(
        string ruta,
        string email,
        string password,
        bool esRegistro,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.Url) || string.IsNullOrWhiteSpace(_options.PublishableKey))
        {
            logger.LogError("La configuración de Supabase Auth está incompleta.");
            return new AuthResult();
        }

        using var solicitud = new HttpRequestMessage(HttpMethod.Post, ruta)
        {
            Content = JsonContent.Create(new { email, password })
        };
        solicitud.Headers.TryAddWithoutValidation("apikey", _options.PublishableKey);

        try
        {
            using var respuesta = await httpClient.SendAsync(solicitud, cancellationToken);
            if (!respuesta.IsSuccessStatusCode)
            {
                logger.LogWarning("Supabase Auth respondió {StatusCode} durante {Operacion}.",
                    (int)respuesta.StatusCode,
                    esRegistro ? "el registro" : "el inicio de sesión");
                return new AuthResult();
            }

            var sesion = await respuesta.Content.ReadFromJsonAsync<SupabaseLoginResponse>(
                new JsonSerializerOptions(JsonSerializerDefaults.Web),
                cancellationToken);

            return new AuthResult
            {
                IsSuccess = sesion is not null,
                RequiresEmailConfirmation = esRegistro && string.IsNullOrWhiteSpace(sesion?.AccessToken),
                AccessToken = sesion?.AccessToken,
                RefreshToken = sesion?.RefreshToken,
                ExpiresIn = sesion?.ExpiresIn ?? 0
            };
        }
        catch (HttpRequestException exception)
        {
            logger.LogError(exception, "No fue posible comunicarse con Supabase Auth.");
            return new AuthResult();
        }
    }
}
