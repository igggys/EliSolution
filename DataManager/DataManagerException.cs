namespace DataManager;

public class DataManagerException : Exception
{
    public DataManagerException(string message)
        : base(message)
    {
    }

    public DataManagerException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
