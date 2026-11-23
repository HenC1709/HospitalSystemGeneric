namespace HospitalSystem.Core.Exceptions;

public class HospitalException(string message, Exception? innerException = null)
    : Exception(message, innerException);

public sealed class HospitalValidationException(string message)
    : HospitalException(message);

public sealed class DatabaseConnectionException(string message, Exception? innerException = null)
    : HospitalException(message, innerException);
