namespace MyThreadPool;

using System.Collections.Concurrent;
using Optional;

public class MyThreadPool : IDisposable
{
    private readonly Thread[] _threads;

    private readonly BlockingCollection<Action> _actionsQueue = new();

    private readonly CancellationTokenSource _cancellationTokenSource = new();

    private bool _isDisposed = false;

    public MyThreadPool(int numberOfThreads)
    {
        _threads = new Thread[numberOfThreads];
        StartThreads();
    }

    public bool IsTerminated { get; private set; }

    public IMyTask<TResult> Submit<TResult>(Func<TResult> func)
    {
        if (IsTerminated)
        {
            throw new Exception(); // сделать другое исключение
        }

        IMyTask<TResult> newTask = new MyTask<TResult>(this, func);

        _actionsQueue.Add((newTask as MyTask<TResult>).MakeExecutableAction());

        return newTask;
    }

    public void Shutdown()
    {
        IsTerminated = true;
        _actionsQueue.CompleteAdding();
        _cancellationTokenSource.Cancel();
    }

    public void Dispose()
    {
        if (!IsTerminated)
        {
            Shutdown();
        }

        if (!_isDisposed)
        {
            _actionsQueue.Dispose();
            _isDisposed = true;
        }
    }

    private static Func<TResult> MakeResultFunction<TResult>(TResult result) =>
        () => result;

    private static Func<TResult> MakeAggregateExceptionFunction<TResult>(Exception innerException) =>
        () => throw new AggregateException(innerException);

    private void StartThreads()
    {
        for (var i = 0; i < _threads.Length; i++)
        {
            _threads[i] = new Thread(() => ThreadActions(_cancellationTokenSource.Token));
            _threads[i].Start();
        }
    }

    private void ThreadActions(CancellationToken cancellationToken)
    {
        foreach (var action in _actionsQueue.GetConsumingEnumerable(cancellationToken))
        {
            action.Invoke();
        }
    }

    private void SubmitContinuationWithoutTasking(Action continuationAction)
    {
        if (IsTerminated)
        {
            throw new Exception(); // сделать другое исключение
        }

        _actionsQueue.Add(continuationAction);
    }

    private class MyTask<TResult> : IMyTask<TResult>
    {
        private readonly MyThreadPool _threadPool;

        private readonly List<Action> _continuationsList = new();

        private readonly Func<TResult> _mainFunction;

        private readonly object _executionLocker = new();

        private Option<Func<TResult>> _resultOption = Option.None<Func<TResult>>();

        public MyTask(MyThreadPool threadPool, Func<TResult> mainFunction)
        {
            _threadPool = threadPool ?? throw new ArgumentException("Thread pool is null.");
            _mainFunction = mainFunction ?? throw new ArgumentException("Function is null.");
        }

        public bool IsCompleted { get; private set; }

        public TResult Result =>
            _resultOption.Match(
                some: resultFunc => resultFunc.Invoke(),
                none: ExecuteRightNow());

        public TResult Execute()
        {
            lock (_executionLocker)
            {
                if (!IsCompleted)
                {
                    var result = _mainFunction.Invoke();
                    return result;
                }
            }

            return Result;
        }

        public IMyTask<TNewResult> ContinueWith<TNewResult>(Func<TResult, TNewResult> continuationFunc)
        {
            if (_threadPool.IsTerminated)
            {
                throw new Exception(); // заменить исключение
            }

            var newTask = new MyTask<TNewResult>(_threadPool, MakeFunctionWithoutArguments(continuationFunc));

            var continuationAction = newTask.MakeExecutableAction();

            lock (_continuationsList)
            {
                if (IsCompleted)
                {
                    _threadPool.SubmitContinuationWithoutTasking(continuationAction);
                }
                else
                {
                    _continuationsList.Add(continuationAction);
                }
            }

            return newTask;
        }

        public Action MakeExecutableAction() => () =>
        {
            try
            {
                var result = Execute();
                _resultOption = MakeResultFunction(result).Some();
            }
            catch (Exception ex)
            {
                _resultOption = MakeAggregateExceptionFunction<TResult>(ex).Some();
            }
            finally
            {
                IsCompleted = true;
                MoveContinuationsToMainQueue();
            }
        };

        private Func<TNewResult> MakeFunctionWithoutArguments<TNewResult>(Func<TResult, TNewResult> oneArgumentFunction) =>
            () => oneArgumentFunction(Result);

        private void MoveContinuationsToMainQueue()
        {
            lock (_continuationsList)
            {
                foreach (var continuation in _continuationsList)
                {
                    _threadPool._actionsQueue.Add(continuation);
                }
            }
        }

        private Func<TResult> ExecuteRightNow()
        {
            lock (_executionLocker)
            {
                if (!IsCompleted)
                {
                    var result = _mainFunction.Invoke();
                    _resultOption = MakeResultFunction(result).Some();
                    IsCompleted = true;
                }
            }

            return _resultOption.ValueOr(() => () => throw new Exception()); // сделать другое исключение
        }
    }
}