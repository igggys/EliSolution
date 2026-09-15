namespace Services.MailService;

public class MailServiceException : Exception
{
    public MailServiceException(string message)
        : base(message)
    {
    }

    public MailServiceException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
