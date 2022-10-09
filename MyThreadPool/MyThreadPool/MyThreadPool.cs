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

        private TResult? _result;

        private readonly object _resultLocker = new();

        private List<Action> _continuations = new();

        public TResult? Result
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
                        
                        lock(_continuations)
                        {
                            foreach (var continuation in _continuations)
                            {
                                _threadPool.Submit(continuation);
                            }
                        }
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
            
            lock(_continuations)
            {
                if (IsCompleted)
                {
                    _threadPool.Submit(action);
                }
                else
                {
                    _continuations.Add(action);
                }
            }
            
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

    private readonly Queue<Action> _queue = new();
    
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
        while (true)
        {
            lock (_queue)
            {
                if (_queue.TryDequeue(out var action))
                {
                    action();
                }
            }
            _numberOfTasks.WaitOne();
        }
    }

    public IMyTask<TResult> Submit<TResult>(Func<TResult> func)
    {
        IMyTask<TResult> newTask = new MyTask<TResult>(this);
        lock (_queue)
        {
            _queue.Enqueue(WrapFunc(newTask, func));
        }
        _numberOfTasks.Release();
        return newTask;
    }

    public void Submit(Action action)
    {
        lock (_queue)
        {
            _queue.Enqueue(action);
        }

        _numberOfTasks.Release();
    }

    private Action WrapFunc<TResult>(IMyTask<TResult> task, Func<TResult> func) => () =>
    {
        var result = func();
        task.Result = result;
    };

    public void Shutdown()
    {
        foreach (var thread in _threads)
        {
            thread.Join();
        }
    }
}