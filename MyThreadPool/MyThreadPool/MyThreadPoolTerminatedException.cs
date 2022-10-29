namespace MyThreadPool;

using System.Runtime.Serialization;

/// <summary>
/// The exception that is thrown in case of an attempt to pass new tasks to <see cref="MyThreadPool"/> or shut it down when it has been already shut down.
/// </summary>
[Serializable]
public class MyThreadPoolTerminatedException : Exception
{
    public MyThreadPoolTerminatedException()
    {
    }

    public MyThreadPoolTerminatedException(string message) : base(message)
    {
    }

    public MyThreadPoolTerminatedException(string message, Exception inner) : base(message, inner)
    {
    }

    protected MyThreadPoolTerminatedException(
        SerializationInfo info,
        StreamingContext context) : base(info, context)
    {
    }
}