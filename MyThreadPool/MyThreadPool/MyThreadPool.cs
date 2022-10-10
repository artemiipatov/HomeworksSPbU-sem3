namespace MyThreadPool;

public class MyThreadPool
{
    private class MyTask<TResult> : IMyTask<TResult>
    {
        public MyTask(MyThreadPool threadPool)
        {
            _threadPool = threadPool;
        }

        private bool _isCompleted;

        private readonly object _locker = new();

        private MyThreadPool _threadPool; // Подумать над другим способом
        
        public bool IsCompleted
        {
            get
            {
                lock (_locker)
                {
                    return _isCompleted;
                }
            }
            set
            {
                lock (_locker)
                {
                    _isCompleted = value;
                }
            }
        }

        private TResult _result;

        private readonly object _resultLocker = new();
        
        public TResult Result
        {
            get
            {
                if (!IsCompleted)
                {
                    lock (_resultLocker)
                    {
                        Monitor.Wait(_resultLocker);
                    }
                }
                
                return _result;
            }
            set
            {
                lock(_resultLocker)
                {
                    if (!IsCompleted)
                    {
                        _result = value;
                        IsCompleted = true;
                        Monitor.Pulse(_resultLocker);
                    }
                }
            }
        }
        
        public IMyTask<TNewResult> ContinueWith<TNewResult>(Func<TResult, TNewResult> continuationFunc)
        {
            IMyTask<TNewResult> newTask = new MyTask<TNewResult>(_threadPool);
            var action = () =>
            {
                var task = newTask;
                task.Result = continuationFunc(Result);
            };
            
            _threadPool.Submit(action, this);
            return newTask;
        }
    }

    public MyThreadPool(int numberOfThreads)
    {
        _threads = new Thread[numberOfThreads];
        StartThreads();
    }

    private readonly Semaphore _numberOfTasks = new(0, 100000); // Либо чем-то заменить, либо подумать над лимитом.

    private readonly Thread[] _threads;

    private readonly List<(Action, Func<bool>?)> _queue = new();

    public bool IsTerminated { get; private set; }

    private void StartThreads()
    {
        for (var i = 0; i < _threads.Length; i++)
        {
            _threads[i] = new Thread(ThreadActions);
            _threads[i].Start();
        }
    }
    
    private void ThreadActions()
    {
        try
        {
            while (true)
            {
                Action? action = null;
                lock (_queue)
                {
                    foreach (var tuple in _queue)
                    {
                        if (tuple.Item2 == null || tuple.Item2())
                        {
                            action = tuple.Item1;
                        }
                    }
                }

                action?.Invoke();
                _numberOfTasks.WaitOne();
            }
        }
        catch (ThreadInterruptedException)
        {
            while (true)
            {
                Action? action = null;
                lock (_queue)
                {
                    foreach (var tuple in _queue)
                    {
                        if (tuple.Item2 == null || tuple.Item2())
                        {
                            action = tuple.Item1;
                        }
                    }
                }

                if (action == null)
                {
                    break;
                }
            }
        }
    }

    public IMyTask<TResult> Submit<TResult>(Func<TResult> func)
    {
        if (IsTerminated)
        {
            throw new Exception(); // сделать другое исключение
        }

        IMyTask<TResult> newTask = new MyTask<TResult>(this);
        lock (_queue)
        {
            _queue.Add((WrapFunc(newTask, func), null));
        }
        _numberOfTasks.Release();
        return newTask;
    }

    private void Submit<TResult>(Action action, IMyTask<TResult> mainTask)
    {
        if (IsTerminated)
        {
            throw new Exception(); // сделать другое исключение
        }
        lock (_queue)
        {
            _queue.Add((action, MakeIsCompletedFunc(mainTask)));
        }

        _numberOfTasks.Release();
    }

    private static Func<bool> MakeIsCompletedFunc<TResult>(IMyTask<TResult> task) => () => task.IsCompleted;
    
    private Action WrapFunc<TResult>(IMyTask<TResult> task, Func<TResult> func) => () =>
    {
        var result = func();
        task.Result = result;
    };

    public void Shutdown()
    {
        IsTerminated = true;
        foreach (var thread in _threads)
        {
            thread.Interrupt();
        }
    }
}