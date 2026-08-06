namespace ControlGastos.Core.DTOs;

public sealed class ApiResponse<T>
{
    public bool IsOk { get; init; }

    public required string Message { get; init; }

    public T? Data { get; init; }
}
