namespace Lazy;

/// <summary>
/// Lazy that does not support multithreading.
/// </summary>
/// <typeparam name="T">Return type of a function.</typeparam>
public class LazySerial<T> : ILazy<T>
{
    public LazySerial(Func<T?> func)
    {
        _func = func;
    }

    private bool _isCalculated = false;
    
    private readonly Func<T?> _func;

    private T? _result;

    public T? Get()
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