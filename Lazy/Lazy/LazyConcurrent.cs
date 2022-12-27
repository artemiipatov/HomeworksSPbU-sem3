namespace Lazy;

/// <summary>
/// Lazy that can be used safely by multiple threads. It guarantees that there won't be any deadlocks or races.
/// </summary>
/// <typeparam name="TResult">Return type of a function.</typeparam>
public class LazyConcurrent<TResult> : ILazy<TResult>
{
    private readonly object _locker = new();

    private volatile bool _isCalculated;

    private Func<TResult?>? _func;

    private TResult? _result;

    /// <summary>
    /// Initializes a new instance of the <see cref="LazyConcurrent{T}"/> class.
    /// </summary>
    /// <param name="func">The delegate to be executed lazily.</param>
    public LazyConcurrent(Func<TResult?> func)
    {
        _func = func;
    }

    /// <inheritdoc/>
    public TResult? Get()
    {
        if (_isCalculated)
        {
            return _result;
        }

        lock (_locker)
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
}