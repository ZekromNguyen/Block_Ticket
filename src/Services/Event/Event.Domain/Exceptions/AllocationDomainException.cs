namespace Event.Domain.Exceptions;

/// <summary>
/// Exception thrown when allocation business rules are violated
/// </summary>
public class AllocationDomainException : DomainException
{
    public AllocationDomainException(string message) : base(message)
    {
    }

    public AllocationDomainException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
