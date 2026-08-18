using System.Text.Json.Serialization;

namespace ControlGastos.Core.DTOs;

public sealed class ReceiptOcrResult
{
    [JsonPropertyName("document_type")]
    public string? DocumentType { get; init; }

    [JsonPropertyName("amount")]
    public decimal? Amount { get; init; }

    [JsonPropertyName("amount_raw")]
    public string? AmountRaw { get; init; }

    [JsonPropertyName("currency")]
    public string? Currency { get; init; }

    // El OCR devuelve una fecha local sin desplazamiento horario.
    [JsonPropertyName("date")]
    public DateTime? Date { get; init; }

    [JsonPropertyName("recipient")]
    public string? Recipient { get; init; }

    [JsonPropertyName("destination")]
    public string? Destination { get; init; }

    [JsonPropertyName("operation_number")]
    public string? OperationNumber { get; init; }

    [JsonPropertyName("confidence")]
    public decimal Confidence { get; init; }

    [JsonPropertyName("requires_review")]
    public bool RequiresReview { get; init; }
}
