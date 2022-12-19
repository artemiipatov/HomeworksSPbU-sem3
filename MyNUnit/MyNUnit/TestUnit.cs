namespace MyNUnit;

using System.Diagnostics;
using System.Reflection;
using Attributes;
using Optional;

public class TestUnit : IPrinter
{
    private readonly TestClass _baseTestClass;

    private readonly MethodInfo _method;

    private readonly List<MethodInfo> _beforeMethods;
    private readonly List<MethodInfo> _afterMethods;

    private readonly object _locker = new ();

    private Option<Type> _expected = Option.None<Type>();
    private string _ignore = string.Empty;

    private Status _status = Status.IsRunning;

    private long _time;

    private string _exceptionInfo = string.Empty;

    private bool _isReady;

    public TestUnit(TestClass baseTestClass, MethodInfo method, List<MethodInfo> beforeMethods, List<MethodInfo> afterMethods)
    {
        _baseTestClass = baseTestClass;
        _method = method;
        _beforeMethods = beforeMethods;
        _afterMethods = afterMethods;

        GetAttributeProperties();
    }

    private enum Status
    {
        Succeed,
        Failed,
        Ignored,
        IsRunning,
    }

    public bool IsReady
    {
        get => _isReady;

        private set
        {
            lock (_locker)
            {
                if (value)
                {
                    _isReady = true;
                    Monitor.PulseAll(_locker);
                }
            }
        }
    }

    public void RunTest()
    {
        if (_ignore.Length != 0)
        {
            _status = Status.Ignored;
            IsReady = true;

            return;
        }

        var watch = Stopwatch.StartNew();

        try
        {
            RunBefore();
            _method.Invoke(_baseTestClass, null);
            RunAfter();

            _status = Status.Succeed;
        }
        catch (Exception exception)
        {
            if (_expected.Match(
                    some: type => exception.GetType().IsAssignableFrom(type),
                    none: () => false))
            {
                _status = Status.Succeed;
            }
            else
            {
                _status = Status.Failed;
                _exceptionInfo = exception.GetType()
                                 + exception.Message
                                 + (exception.StackTrace ?? string.Empty);
            }
        }

        watch.Stop();
        _time = watch.ElapsedMilliseconds;

        IsReady = true;
    }

    public void Print()
    {
        lock (_locker)
        {
            if (!IsReady)
            {
                Monitor.Wait(_locker);
            }
        }

        Console.WriteLine(_method.Name);

        var message = _status switch
        {
            Status.Succeed => $"Succeed. Time: {_time} ms.",
            Status.Failed => $"Failed. Time: {_time} ms."
                             + Environment.NewLine
                             + _exceptionInfo,
            Status.Ignored => $"Ignored. Reason: {_ignore}",
            _ => throw new ArgumentOutOfRangeException()
        };

        Console.WriteLine(message);
    }

    private void RunBefore()
    {
        foreach (var before in _beforeMethods)
        {
            before.Invoke(_baseTestClass, null);
        }
    }

    private void RunAfter()
    {
        foreach (var after in _afterMethods)
        {
            after.Invoke(_baseTestClass, null);
        }
    }

    private void GetAttributeProperties()
    {
        var testAttribute = (TestAttribute)Attribute
            .GetCustomAttributes(_method).First(attr => attr.GetType() == typeof(TestAttribute));

        _expected = testAttribute.Expected;
        _ignore = testAttribute.Ignore;
    }
}