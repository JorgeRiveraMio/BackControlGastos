using ControlGastos.Core.DTOs;

namespace ControlGastos.Core.Interfaces;

public interface IReceiptOcrService
{
    Task<ReceiptOcrResult> AnalyzeAsync(
        Stream image,
        string fileName,
        string? contentType,
        CancellationToken cancellationToken);
}
