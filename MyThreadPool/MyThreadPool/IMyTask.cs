namespace MyThreadPool;

/// <summary>
/// Represents an asynchronous operation that can return a value.
/// </summary>
/// <typeparam name="TResult">Result type of the task.</typeparam>
public interface IMyTask<TResult>
{
    /// <summary>
    /// Gets a value indicating whether the task is computed.
    /// </summary>
    /// <returns>Returns true if the task has been computed; otherwise, false.</returns>
    bool IsCompleted { get; }

    /// <summary>
    /// Gets the result of the task instantly if it has been computed; otherwise, computes it and then returns the result.
    /// </summary>
    /// <returns>The result value of this <see cref="IMyTask{TResult}"/>, which is of the same type as the task's type parameter.</returns>
    TResult Result { get; }

    /// <summary>
    /// Creates a continuation that executes asynchronously when the target <see cref="IMyTask{TResult}"/> completes.
    /// </summary>
    /// <param name="continuationFunc">Continuation function that should be computed.</param>
    /// <typeparam name="TNewResult">Result type of the continuation task.</typeparam>
    /// <returns>A new continuation <see cref="IMyTask{TResult}"/>.</returns>
    IMyTask<TNewResult> ContinueWith<TNewResult>(Func<TResult, TNewResult> continuationFunc);
}