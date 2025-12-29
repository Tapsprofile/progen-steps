namespace Procurement.Orchestration.Core.Exceptions;

public abstract class DomainException(string message) : Exception(message);

public sealed class ValidationException(string message) : DomainException(message);

public sealed class NotFoundException(string message) : Exception(message);

