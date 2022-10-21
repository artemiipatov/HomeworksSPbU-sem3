namespace MyThreadPool;

public interface IMyTask<TResult>
{
    bool IsCompleted { get; }

    TResult Result { get; set; }

    IMyTask<TNewResult> ContinueWith<TNewResult>(Func<TResult, TNewResult> continuationFunc);
}