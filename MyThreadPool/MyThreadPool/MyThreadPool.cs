namespace MyThreadPool;

using System.Collections.Concurrent;
using Optional;

/// <summary>
/// Provides a pool of threads that can be used to execute tasks, continuation tasks. Implements IDisposable.
/// </summary>
public class MyThreadPool : IDisposable
{
    private readonly Thread[] _threads;

    private readonly BlockingCollection<Action> _actionsQueue = new();

    private readonly CancellationTokenSource _cancellationTokenSource = new();

    private bool _isDisposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="MyThreadPool"/> class.
    /// </summary>
    /// <param name="numberOfThreads">Amount of threads on thread pool.</param>
    public MyThreadPool(int numberOfThreads)
    {
        _threads = new Thread[numberOfThreads];
        StartThreads();
    }

    /// <summary>
    /// Gets a value indicating whether <see cref="MyThreadPool"/> is shutdown.
    /// </summary>
    /// <returns>True if <see cref="MyThreadPool"/> is shutdown; otherwise, false.</returns>
    public bool IsTerminated { get; private set; }

    /// <summary>
    /// Adds new task to this <see cref="MyThreadPool"/>.
    /// </summary>
    /// <param name="func">Function that should be calculated.</param>
    /// <typeparam name="TResult">Result type of the <paramref name="func"/>.</typeparam>
    /// <returns>A new task <see cref="IMyTask{TResult}"/>.</returns>
    /// <exception cref="MyThreadPoolTerminatedException">Throws if <see cref="MyThreadPool"/> is shut down.</exception>
    public IMyTask<TResult> Submit<TResult>(Func<TResult> func)
    {
        if (IsTerminated)
        {
            throw new MyThreadPoolTerminatedException();
        }

        IMyTask<TResult> newTask = new MyTask<TResult>(this, func);

        _actionsQueue.Add((newTask as MyTask<TResult>).MakeExecutableAction());

        return newTask;
    }

    /// <summary>
    /// Terminates this <see cref="MyThreadPool"/>.
    /// </summary>
    /// <exception cref="MyThreadPoolTerminatedException">Throws if this <see cref="MyThreadPool"/> is already shut down.</exception>
    public void Shutdown()
    {
        if (IsTerminated)
        {
            throw new MyThreadPoolTerminatedException("Thread pool is already shut down.");
        }

        IsTerminated = true;
        _cancellationTokenSource.Cancel();
        foreach (var thread in _threads)
        {
            thread.Join();
        }
    }

    /// <summary>
    /// Releases all resources used by the current instance of the <see cref="MyThreadPool"/>> class.
    /// </summary>
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
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var action = _actionsQueue.Take(cancellationToken);

                action.Invoke();
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void SubmitContinuationWithoutTasking(Action continuationAction)
    {
        if (IsTerminated)
        {
            throw new MyThreadPoolTerminatedException("Thread pool was shut down.");
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

        /// <summary>
        /// Initializes a new instance of the <see cref="MyTask{TResult}"/> class.
        /// </summary>
        /// <param name="threadPool"><see cref="MyThreadPool"/> which will compute the task.</param>
        /// <param name="mainFunction">Function that should be computed.</param>
        /// <exception cref="ArgumentException">Throws if <paramref name="threadPool"/> or <paramref name="mainFunction"/> is null.</exception>
        public MyTask(MyThreadPool threadPool, Func<TResult> mainFunction)
        {
            _threadPool = threadPool ?? throw new ArgumentException("Thread pool is null.");
            _mainFunction = mainFunction ?? throw new ArgumentException("Function is null.");
        }

        /// <inheritdoc />
        public bool IsCompleted { get; private set; }

        /// <inheritdoc />
        public TResult Result =>
            _resultOption.Match(
                some: resultFunc => resultFunc.Invoke(),
                none: ExecuteRightNow());

        /// <inheritdoc />
        public IMyTask<TNewResult> ContinueWith<TNewResult>(Func<TResult, TNewResult> continuationFunc)
        {
            if (_threadPool.IsTerminated)
            {
                throw new MyThreadPoolTerminatedException(); // заменить исключение
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

        /// <summary>
        /// Makes action that can be passed to <see cref="MyThreadPool"/>
        /// </summary>
        /// <returns>Action that wraps main function of the task. This action computes main function and writes the result of it to the <see cref="Result"/> of the <see cref="MyTask{TResult}"/>.</returns>
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

        private TResult Execute()
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