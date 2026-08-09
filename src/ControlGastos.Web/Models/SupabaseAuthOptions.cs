namespace ControlGastos.Web.Models;

public sealed class SupabaseAuthOptions
{
    public const string SectionName = "SupabaseAuth";

    public string Url { get; init; } = string.Empty;

    public string PublishableKey { get; init; } = string.Empty;
}
