namespace ControlGastos.Web.Models.Responses;

public sealed class ApiResponse<T>
{
    public bool IsOk { get; init; }

    public string Message { get; init; } = string.Empty;

    public T? Data { get; init; }
}
