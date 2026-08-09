namespace ControlGastos.Core.Interfaces;

public interface IEvaluadorPresupuestoService
{
    Task EvaluarAsync(Guid idUsuario, DateTimeOffset fechaGasto, CancellationToken cancellationToken);
}
