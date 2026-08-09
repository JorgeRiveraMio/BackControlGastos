namespace ControlGastos.Web.Models.Responses;

public sealed class AuthResult
{
    public bool IsSuccess { get; init; }

    public bool RequiresEmailConfirmation { get; init; }

    public string? AccessToken { get; init; }

    public string? RefreshToken { get; init; }

    public int ExpiresIn { get; init; }
}
