namespace Lazy;

/// <summary>
/// Lazy that can be used concurrently by multiple threads. It guarantees that there won't be any deadlocks or races.
/// </summary>
/// <typeparam name="T">Return type of a function.</typeparam>
public class LazyConcurrent<T> : ILazy<T>
{
    public LazyConcurrent(Func<T?> func)
    {
        _func = func;
    }

    private bool _isCalculated;
    
    private object _locker = new();
    
    private Func<T?> _func;

    private T? _result;
    
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