namespace MyNUnit.Internal;

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using Attributes;
using Printer;
using Optional;

/// <summary>
/// Abstraction for running Before, Test, After methods. Contains information of test run.
/// </summary>
public class TestUnit
{
    private readonly object _baseTestClass;

    private readonly List<MethodInfo> _beforeMethods;
    private readonly List<MethodInfo> _afterMethods;

    private readonly object _locker = new ();

    private BlockingCollection<Exception> _exceptions = new ();

    private Option<Type> _expected = Option.None<Type>();

    private bool _isReady;

    private TestUnitStatus _status = TestUnitStatus.IsRunning;

    /// <summary>
    /// Initializes a new instance of the <see cref="TestUnit"/> class.
    /// </summary>
    /// <param name="baseTestClass">An instance of class where current method is running.</param>
    /// <param name="method">Method with <see cref="TestAttribute"/>.</param>
    /// <param name="beforeMethods">List of methods with <see cref="BeforeAttribute"/>.</param>
    /// <param name="afterMethods">List of methods with <see cref="AfterAttribute"/>.</param>
    public TestUnit(object baseTestClass, MethodInfo method, List<MethodInfo> beforeMethods, List<MethodInfo> afterMethods)
    {
        _baseTestClass = baseTestClass;
        Method = method;
        _beforeMethods = beforeMethods;
        _afterMethods = afterMethods;

        GetAttributeProperties();
    }

    /// <summary>
    /// Gets <see cref="MethodInfo"/> of current <see cref="TestAttribute"/> method.
    /// </summary>
    public MethodInfo Method { get; }

    /// <summary>
    /// Gets optional value -- reason for ignoring current method.
    /// If the value is missing, method should not be ignored.
    /// </summary>
    public Option<string> Ignore { get; private set; } = Option.None<string>();

    /// <summary>
    /// Gets name of expected exception.
    /// If expected exception is not given, returns empty string.
    /// </summary>
    public string ExpectedExceptionName => _expected.Match(
        some: exceptionType => exceptionType.ToString(),
        none: () => string.Empty);

    /// <summary>
    /// Gets test run time.
    /// </summary>
    public long Time { get; private set; }

    /// <summary>
    /// Gets information of caught unexpected exceptions.
    /// If no unexpected exceptions was caught, returns empty string.
    /// </summary>
    public string ExceptionInfo => _exceptions.Count == 0 ?
        string.Empty :
        new AggregateException(_exceptions).ToString();

    /// <summary>
    /// Gets status of current test run.
    /// </summary>
    public TestUnitStatus Status => _status;

    /// <summary>
    /// Gets a value indicating whether test run finished.
    /// </summary>
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

    /// <summary>
    /// Runs test unit.
    /// </summary>
    public void Run()
    {
        if (IsReady)
        {
            return;
        }

        if (!CheckAllMethods())
        {
            IsReady = true;

            return;
        }

        var watch = Stopwatch.StartNew();

        if (!RunBeforeMethods())
        {
            watch.Stop();
            Time = watch.ElapsedMilliseconds;

            SetStatus(TestUnitStatus.BeforeFailed);
            IsReady = true;

            return;
        }

        TryRunTest();
        RunAfterMethods();

        watch.Stop();
        Time = watch.ElapsedMilliseconds;

        if (_status == TestUnitStatus.IsRunning)
        {
            SetStatus(TestUnitStatus.Succeed);
        }

        IsReady = true;
    }

    /// <summary>
    /// Accepts <see cref="IPrinter"/> instance.
    /// </summary>
    /// <param name="printer">Objects that implements <see cref="IPrinter"/>.</param>
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

        return true;
    }

    private void RunAfterMethods()
    {
        Parallel.ForEach(_afterMethods, after =>
        {
            TryRunAfter(after);
        });
    }

    private bool TryRunBefore(MethodInfo before)
    {
        try
        {
            before.Invoke(_baseTestClass, null);
            return true;
        }
        catch (TargetInvocationException exception)
        {
            _exceptions.Add(exception);
            SetStatus(TestUnitStatus.BeforeFailed);
            return false;
        }
    }

    private bool TryRunAfter(MethodInfo after)
    {
        try
        {
            after.Invoke(_baseTestClass, null);
            return true;
        }
        catch (TargetInvocationException exception)
        {
            _exceptions.Add(exception);
            SetStatus(TestUnitStatus.AfterFailed);
            return false;
        }
    }

    private bool TryRunTest()
    {
        try
        {
            Method.Invoke(_baseTestClass, null);
            return true;
        }
        catch (TargetInvocationException exception)
        {
            if (_expected.Match(
                    some: type => exception.InnerException?.GetType() == type,
                    none: () => false))
            {
                SetStatus(TestUnitStatus.CaughtExpectedException);
                return true;
            }

            _exceptions.Add(exception);
            SetStatus(TestUnitStatus.TestFailed);
            return false;
        }
    }

    private bool CheckAllMethods() =>
        CheckMethodSignature(Method)
        && (_beforeMethods.Count == 0
            || _beforeMethods.Any(CheckMethodSignature))
        && (_afterMethods.Count == 0
            || _afterMethods.Any(CheckMethodSignature));

    private bool CheckMethodSignature(MethodInfo method)
    {
        if (Ignore.HasValue)
        {
            SetStatus(TestUnitStatus.Ignored);

            return false;
        }

        var methodSignature = new MethodSignature(method);

        var previousStatus = _status;

        _status = methodSignature switch
        {
            { IsPublic: false } => TestUnitStatus.NonPublicMethod,
            { IsStatic: true } => TestUnitStatus.StaticMethod,
            { HasArguments: true } => TestUnitStatus.MethodHasArguments,
            { IsVoid: false } => TestUnitStatus.NonVoidMethod,
            _ => _status,
        };

        return _status == previousStatus;
    }

    private void SetStatus(TestUnitStatus status) => _status = status;

    private void GetAttributeProperties()
    {
        var testAttribute = (TestAttribute)Attribute
            .GetCustomAttributes(Method)
            .First(attr => attr.GetType() == typeof(TestAttribute));

        _expected = testAttribute.Expected;
        Ignore = testAttribute.Ignore;
    }
}