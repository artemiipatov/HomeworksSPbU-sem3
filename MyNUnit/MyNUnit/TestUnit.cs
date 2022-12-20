namespace MyNUnit;

using System.Diagnostics;
using System.Reflection;
using Attributes;
using Optional;

public class TestUnit
{
    private readonly object _baseTestClass;

    private readonly List<MethodInfo> _beforeMethods;
    private readonly List<MethodInfo> _afterMethods;

    private readonly object _locker = new ();

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

    public string Ignore { get; private set; } = string.Empty;

    public Status CurrentStatus { get; private set; } = Status.IsRunning;

    public long Time { get; private set; }

    public string ExceptionInfo { get; private set; } = string.Empty;

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

    public void RunTest()
    {
        if (Ignore.Length != 0)
        {
            CurrentStatus = Status.Ignored;
            IsReady = true;

            return;
        }

        var watch = Stopwatch.StartNew();

        try
        {
            RunBefore();
            Method.Invoke(_baseTestClass, null);
            RunAfter();

            CurrentStatus = Status.Succeed;
        }
        catch (Exception exception)
        {
            if (_expected.Match(
                    some: type => exception.GetType().IsAssignableFrom(type),
                    none: () => false))
            {
                CurrentStatus = Status.Succeed;
            }
            else
            {
                CurrentStatus = Status.Failed;
                ExceptionInfo = exception.ToString();
            }
        }

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
            .GetCustomAttributes(Method).First(attr => attr.GetType() == typeof(TestAttribute));

        _expected = testAttribute.Expected;
        Ignore = testAttribute.Ignore;
    }
}