namespace ControlGastos.Infrastructure.Storage;

public sealed class SupabaseOptions
{
    public const string SectionName = "Supabase";

    public string Url { get; init; } = string.Empty;

    public string SecretKey { get; init; } = string.Empty;

    public string ComprobantesBucket { get; init; } = string.Empty;
}
