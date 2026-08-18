namespace ControlGastos.Core.Exceptions;

public sealed class OcrServiceException(string message, Exception? innerException = null)
    : Exception(message, innerException);
