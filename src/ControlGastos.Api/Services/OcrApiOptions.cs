namespace ControlGastos.Api.Services;

public sealed class OcrApiOptions
{
    public const string SectionName = "OcrApi";

    public string BaseUrl { get; init; } = string.Empty;

    public int TimeoutSeconds { get; init; } = 120;
}
