namespace Client;

using System.Runtime.Serialization;

[Serializable]
public class DataLossException : Exception
{

    public DataLossException()
    {
    }

    public DataLossException(string message) : base(message)
    {
    }

    public DataLossException(string message, Exception inner) : base(message, inner)
    {
    }

    protected DataLossException(
        SerializationInfo info,
        StreamingContext context) : base(info, context)
    {
    }
}