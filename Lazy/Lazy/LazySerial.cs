namespace Lazy;

/// <summary>
/// Lazy that does not support multithreading.
/// </summary>
/// <typeparam name="T">Return type of a function.</typeparam>
public class LazySerial<T> : ILazy<T>
{
    private Func<T?>? _func;

    private bool _isCalculated;

    private T? _result;

    /// <summary>
    /// Initializes a new instance of the <see cref="LazySerial{T}"/> class.
    /// </summary>
    /// <param name="func">The delegate to be executed lazily.</param>
    public LazySerial(Func<T?> func)
    {
        _func = func;
    }

    /// <inheritdoc/>
    public T? Get()
    {
        if (_isCalculated)
        {
            return _result;
        }

        _result = _func!(); // It cannot be null because argument is not nullable.
        _func = null;
        _isCalculated = true;
        return _result;
    }
}