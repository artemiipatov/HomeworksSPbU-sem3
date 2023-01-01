namespace Client.Exceptions;

/// <summary>
/// The exception that is thrown in case of data loss.
/// </summary>
[Serializable]
public class DataLossException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataLossException"/> class.
    /// </summary>
    public DataLossException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DataLossException"/> class.
    /// </summary>
    /// <param name="message">Exception message.</param>
    public DataLossException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DataLossException"/> class.
    /// </summary>
    /// <param name="message">Exception message.</param>
    /// <param name="inner">Inner exception.</param>
    public DataLossException(string message, Exception inner)
        : base(message, inner)
    {
    }
}