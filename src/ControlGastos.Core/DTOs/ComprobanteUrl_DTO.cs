namespace ControlGastos.Core.DTOs;

public sealed class ComprobanteUrl_DTO
{
    public required string Url { get; init; }

    public int ExpiraEnSegundos { get; init; }
}
