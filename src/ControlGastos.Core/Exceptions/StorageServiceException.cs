namespace ControlGastos.Core.Exceptions;

public sealed class StorageServiceException(string message, Exception? innerException = null)
    : Exception(message, innerException);
