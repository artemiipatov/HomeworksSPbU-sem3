using MyThreadPool.Exceptions;

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
        if (numberOfThreads <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(numberOfThreads));
        }

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

        var newTask = new MyTask<TResult>(this, func, _cancellationTokenSource.Token);

        try
        {
            _actionsQueue.Add(newTask.MakeExecutableAction(), _cancellationTokenSource.Token);
        }
        catch (OperationCanceledException)
        {
            throw new MyThreadPoolTerminatedException("Thread pool is shut down.");
        }

        return newTask;
    }

    /// <summary>
    /// Terminates <see cref="MyThreadPool"/>.
    /// </summary>
    /// <exception cref="MyThreadPoolTerminatedException">Throws if <see cref="MyThreadPool"/> is already shut down.</exception>
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
    /// Releases all resources used by the current instance of the <see cref="MyThreadPool"/> class.
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

        try
        {
            _actionsQueue.Add(continuationAction, _cancellationTokenSource.Token);
        }
        catch (OperationCanceledException)
        {
            throw new MyThreadPoolTerminatedException("Thread pool is shut down.");
        }
    }

    private class MyTask<TResult> : IMyTask<TResult>
    {
        private readonly MyThreadPool _threadPool;

        private readonly BlockingCollection<Action> _continuations = new();

        private readonly Func<TResult> _mainFunction;

        private readonly object _executionLocker = new();

        private readonly CancellationToken _token;

        private Option<TResult> _resultOption = Option.None<TResult>();

        private Option<AggregateException> _exception = Option.None<AggregateException>();

        private bool _isCompleted;

        /// <summary>
        /// Initializes a new instance of the <see cref="MyTask{TResult}"/> class.
        /// </summary>
        /// <param name="threadPool"><see cref="MyThreadPool"/> which will compute the task.</param>
        /// <param name="mainFunction">Function that should be computed.</param>
        /// <param name="cancellationToken">A token that can be used to cancel operations.</param>
        /// <exception cref="ArgumentException">Throws if <paramref name="threadPool"/> or <paramref name="mainFunction"/> is null.</exception>
        public MyTask(MyThreadPool threadPool, Func<TResult> mainFunction, CancellationToken cancellationToken)
        {
            _threadPool = threadPool ?? throw new ArgumentNullException();
            _mainFunction = mainFunction ?? throw new ArgumentNullException();
            _token = cancellationToken;
        }

        /// <inheritdoc />
        public bool IsCompleted
        {
            get => _isCompleted;

            private set
            {
                lock (_executionLocker)
                {
                    _isCompleted = value;
                    Monitor.PulseAll(_executionLocker);
                }
            }
        }

        /// <inheritdoc />
        public TResult Result =>
            _exception.Match(
                some: exception => throw exception,
                none: () =>
            _resultOption.Match(
                some: result => result,
                none: ExecuteRightNow));

        /// <inheritdoc />
        public IMyTask<TNewResult> ContinueWith<TNewResult>(Func<TResult, TNewResult> continuationFunc)
        {
            if (_threadPool.IsTerminated)
            {
                throw new MyThreadPoolTerminatedException();
            }

            var newTask = new MyTask<TNewResult>(_threadPool, MakeFunctionWithoutArguments(continuationFunc), _token);

            var continuationAction = newTask.MakeExecutableAction();

            lock (_continuations)
            {
                if (IsCompleted)
                {
                    _threadPool.SubmitContinuationWithoutTasking(continuationAction);
                }
                else
                {
                    try
                    {
                        _continuations.Add(continuationAction, _token);
                    }
                    catch (OperationCanceledException)
                    {
                        throw new MyThreadPoolTerminatedException();
                    }
                }
            }

            return newTask;
        }

        /// <summary>
        /// Makes action that can be passed to <see cref="MyThreadPool"/>.
        /// </summary>
        /// <returns>Action that wraps main function of the task. This action computes main function and writes the result of it to the <see cref="Result"/> of the <see cref="MyTask{TResult}"/>.</returns>
        public Action MakeExecutableAction() => () =>
        {
            try
            {
                var result = Execute();
                _resultOption = result.Some();
            }
            catch (Exception exception)
            {
                _exception = new AggregateException(exception).Some();
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
            lock (_continuations)
            {
                try
                {
                    foreach (var continuation in _continuations)
                    {
                        _threadPool.SubmitContinuationWithoutTasking(continuation);
                    }
                }
                catch (MyThreadPoolTerminatedException)
                {
                }
            }
        }

        private TResult ExecuteRightNow()
        {
            lock (_executionLocker)
            {
                if (IsCompleted)
                {
                    return Result;
                }

                if (_threadPool.IsTerminated)
                {
                    var exception = new AggregateException(new MyThreadPoolTerminatedException());
                    _exception = exception.Some();
                    throw exception;
                }

                var result = _mainFunction.Invoke();
                _resultOption = result.Some();
                IsCompleted = true;
            }

            return Result;
        }
    }
}