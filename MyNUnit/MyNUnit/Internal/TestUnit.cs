namespace MyNUnit.Internal;

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using Attributes;
using Printer;
using Optional;

public class TestUnit
{
    private readonly object _baseTestClass;

    private readonly List<MethodInfo> _beforeMethods;
    private readonly List<MethodInfo> _afterMethods;

    private readonly object _locker = new ();

    private BlockingCollection<Exception> _exceptions = new ();

    private Option<Type> _expected = Option.None<Type>();

    private bool _isReady;

    public TestUnit(object baseTestClass, MethodInfo method, List<MethodInfo> beforeMethods, List<MethodInfo> afterMethods)
    {
        _baseTestClass = baseTestClass;
        Method = method;
        _beforeMethods = beforeMethods;
        _afterMethods = afterMethods;

        GetAttributeProperties();
    }

    public MethodInfo Method { get; }

    public Option<string> Ignore { get; private set; } = Option.None<string>();

    public long Time { get; private set; }

    public string ExceptionInfo => _exceptions.Count == 0 ?
        string.Empty :
        new AggregateException(_exceptions).ToString();

    public Status GeneralStatus
    {
        get
        {
            if (BeforeStatus == Status.IsRunning
                || TestStatus == Status.IsRunning
                || AfterStatus == Status.IsRunning)
            {
                return Status.IsRunning;
            }

            if (BeforeStatus != Status.Succeed
                || (TestStatus != Status.Succeed
                 && TestStatus != Status.CaughtExpectedException)
                || AfterStatus != Status.Succeed)
            {
                return Status.Failed;
            }

            return Status.Succeed;
        }
    }

    public Status BeforeStatus { get; private set; } = Status.IsRunning;

    public Status TestStatus { get; private set; } = Status.IsRunning;

    public Status AfterStatus { get; private set; } = Status.IsRunning;

    public bool IsReady
    {
        get => _isReady;

        private set
        {
            if (!value)
            {
                return;
            }

            lock (_locker)
            {
                _isReady = true;
                Monitor.PulseAll(_locker);
            }
        }
    }

    public void Run()
    {
        if (Ignore.HasValue)
        {
            TestStatus = Status.Ignored;
            BeforeStatus = Status.Ignored;
            AfterStatus = Status.Ignored;
            IsReady = true;

            return;
        }

        TestStatus = CheckMethodSignature(Method, TestStatus);
        if (TestStatus != Status.IsRunning)
        {
            BeforeStatus = Status.Ignored;
            AfterStatus = Status.Ignored;
            IsReady = true;

            return;
        }

        var watch = Stopwatch.StartNew();

        if (!RunBeforeMethods())
        {
            watch.Stop();
            Time = watch.ElapsedMilliseconds; // Сделать extension к секундомеру.

            TestStatus = Status.Ignored;
            AfterStatus = Status.Ignored;
            IsReady = true;

            return;
        }

        TryRunTest();
        RunAfterMethods();

        watch.Stop();
        Time = watch.ElapsedMilliseconds;

        IsReady = true;
    }

    public void AcceptPrinter(IPrinter printer)
    {
        lock (_locker)
        {
            if (!IsReady)
            {
                Monitor.Wait(_locker);
            }
        }

        printer.PrintTestUnitInfo(this);
    }

    private bool RunBeforeMethods()
    {
        foreach (var before in _beforeMethods)
        {
            if (!TryRunBefore(before))
            {
                return false;
            }
        }

        BeforeStatus = Status.Succeed;

        return true;
    }

    private void RunAfterMethods()
    {
        Parallel.ForEach(_afterMethods, after =>
        {
            TryRunAfter(after);
        });

        if (AfterStatus == Status.IsRunning)
        {
            AfterStatus = Status.Succeed;
        }
    }

    private bool TryRunBefore(MethodInfo before)
    {
        BeforeStatus = CheckMethodSignature(before, BeforeStatus);
        if (BeforeStatus != Status.IsRunning)
        {
            return false;
        }

        try
        {
            before.Invoke(_baseTestClass, null);
            return true;
        }
        catch (TargetInvocationException exception)
        {
            _exceptions.Add(exception);
            BeforeStatus = Status.Failed;
            return false;
        }
    }

    private bool TryRunAfter(MethodInfo after)
    {
        AfterStatus = CheckMethodSignature(after, AfterStatus);
        if (AfterStatus != Status.IsRunning)
        {
            return false;
        }

        try
        {
            after.Invoke(_baseTestClass, null);
            return true;
        }
        catch (TargetInvocationException exception)
        {
            _exceptions.Add(exception);
            AfterStatus = Status.Failed;
            return false;
        }
    }

    private bool TryRunTest()
    {
        try
        {
            Method.Invoke(_baseTestClass, null);
            TestStatus = Status.Succeed;
            return true;
        }
        catch (TargetInvocationException exception)
        {
            if (_expected.Match(
                    some: type => exception.GetType().IsAssignableFrom(type),
                    none: () => false))
            {
                TestStatus = Status.CaughtExpectedException;
                return true;
            }
            else
            {
                _exceptions.Add(exception);
                TestStatus = Status.Failed;
                return false;
            }
        }
    }

    private Status CheckMethodSignature(MethodInfo method, Status status)
    {
        var methodSignature = new MethodSignature(method);
        return methodSignature switch
        {
            { IsPublic: false } => Status.NonPublicMethod,
            { IsStatic: true } => Status.StaticMethod,
            { HasArguments: true } => Status.MethodHasArguments,
            { IsVoid: false } => Status.NonVoidMethod,
            _ => status,
        };
    }

    private void GetAttributeProperties()
    {
        var testAttribute = (TestAttribute)Attribute
            .GetCustomAttributes(Method)
            .First(attr => attr.GetType() == typeof(TestAttribute));

        _expected = testAttribute.Expected;
        Ignore = testAttribute.Ignore;
    }
}