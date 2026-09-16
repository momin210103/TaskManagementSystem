namespace TaskManagement.Application.Exceptions;

public class DuplicateEmailException : Exception
{
    public DuplicateEmailException()
    {
    }

    public DuplicateEmailException(string message)
        : base(message)
    {
    }

    public DuplicateEmailException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

