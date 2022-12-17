namespace MyThreadPool.Exceptions;

/// <summary>
/// The exception that is thrown in case of an attempt to pass new tasks to <see cref="MyThreadPool"/> or shut it down when it has been already shut down.
/// </summary>
[Serializable]
public class MyThreadPoolTerminatedException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MyThreadPoolTerminatedException"/> class.
    /// </summary>
    public MyThreadPoolTerminatedException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MyThreadPoolTerminatedException"/> class.
    /// </summary>
    /// <param name="message">Exception message.</param>
    public MyThreadPoolTerminatedException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MyThreadPoolTerminatedException"/> class.
    /// </summary>
    /// <param name="message">Exception message.</param>
    /// <param name="inner">Inner exception.</param>
    public MyThreadPoolTerminatedException(string message, Exception inner)
        : base(message, inner)
    {
    }
}