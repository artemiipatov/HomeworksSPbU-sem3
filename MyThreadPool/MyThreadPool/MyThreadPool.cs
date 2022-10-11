﻿namespace MyThreadPool;

public class MyThreadPool
{
    private readonly Semaphore _numberOfTasks = new(0, 100000); // Либо чем-то заменить, либо подумать над лимитом.

    private readonly Thread[] _threads;

    private readonly List<(Action, Func<bool>?)> _actionsList = new();

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

        IMyTask<TResult> newTask = new MyTask<TResult>(this);
        lock (_actionsList)
        {
            _actionsList.Add((WrapFunc(newTask, func), null));
        }

        _numberOfTasks.Release();
        return newTask;
    }

    public void Shutdown()
    {
        IsTerminated = true;
        foreach (var thread in _threads)
        {
            thread.Interrupt();
        }
    }

    private static Func<bool> MakeIsCompletedFunc<TResult>(IMyTask<TResult> task) => () => task.IsCompleted;

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
                lock (_actionsList)
                {
                    var counter = -1;
                    foreach (var tuple in _actionsList)
                    {
                        counter++;
                        if (tuple.Item2 == null || tuple.Item2())
                        {
                            action = tuple.Item1;
                            _actionsList.RemoveAt(counter);
                            break;
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
                lock (_actionsList)
                {
                    foreach (var tuple in _actionsList)
                    {
                        if (tuple.Item2 == null || tuple.Item2())
                        {
                            action = tuple.Item1;
                            break;
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

    private void Submit<TResult>(Action action, IMyTask<TResult> mainTask)
    {
        if (IsTerminated)
        {
            throw new Exception(); // сделать другое исключение
        }

        lock (_actionsList)
        {
            _actionsList.Add((action, MakeIsCompletedFunc(mainTask)));
        }

        _numberOfTasks.Release();
    }

    private Action WrapFunc<TResult>(IMyTask<TResult> task, Func<TResult> func) => () =>
    {
        var result = func();
        task.Result = result;
    };

    private class MyTask<TResult> : IMyTask<TResult>
    {
        private readonly MyThreadPool _threadPool; // Подумать над другим способом

        private readonly object _locker = new();

        private readonly object _resultLocker = new();

        private bool _isCompleted;

        private TResult _result;

        public MyTask(MyThreadPool threadPool)
        {
            _threadPool = threadPool;
        }

        public bool IsCompleted
        {
            get
            {
                lock (_locker)
                {
                    return _isCompleted;
                }
            }

            private set
            {
                lock (_locker)
                {
                    _isCompleted = value;
                }
            }
        }

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
                lock (_resultLocker)
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
}