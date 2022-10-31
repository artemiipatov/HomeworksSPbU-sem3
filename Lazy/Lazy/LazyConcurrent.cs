namespace Lazy;

/// <summary>
/// Lazy that can be used safely by multiple threads. It guarantees that there won't be any deadlocks or races.
/// </summary>
/// <typeparam name="T">Return type of a function.</typeparam>
public class LazyConcurrent<T> : ILazy<T>
{
    private readonly object _locker = new();

    private bool _isCalculated;

    private Func<T?> _func;

    private T? _result;

    /// <summary>
    /// Initializes a new instance of the <see cref="LazyConcurrent{T}"/> class.
    /// </summary>
    /// <param name="func">The delegate to be executed lazily.</param>
    public LazyConcurrent(Func<T?> func)
    {
        _func = func;
    }

    /// <inheritdoc/>
    public T? Get()
    {
        lock (_locker)
        {
            if (_isCalculated)
            {
                return _result;
            }

            _result = _func();
            _isCalculated = true;
            return _result;
        }
    }
}