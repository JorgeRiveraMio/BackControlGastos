namespace ControlGastos.Web.Models.Responses;

public sealed class ApiClientResult<T>
{
    public bool IsSuccess { get; init; }

    public bool IsUnauthorized { get; init; }

    public T? Data { get; init; }
}
