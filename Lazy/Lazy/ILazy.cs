namespace Lazy;

/// <summary>
/// Defers the execution of given function. Function executes only once and only when Get() is called.
/// </summary>
/// <typeparam name="T">Return type of a function.</typeparam>
public interface ILazy<T>
{
    /// <summary>
    /// Executes function and return the result if Get() is called for the first time. If Get() is called not for the first time, it just returns the result of the first execution. 
    /// </summary>
    /// <returns>The result of the first function execution.</returns>
    T? Get();
}