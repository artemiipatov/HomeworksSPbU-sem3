namespace MyNUnit.Internal;

using System.Collections.Concurrent;
using System.Reflection;
using Attributes;
using Printer;

public class TestType
{
    private readonly object _classInstance;

    private readonly List<TestUnit> _testUnitList = new ();

    private readonly object _locker = new ();

    private BlockingCollection<Exception> _exceptions = new ();

    private List<MethodInfo> _testMethods = new ();
    private List<MethodInfo> _beforeMethods = new ();
    private List<MethodInfo> _afterMethods = new ();
    private List<MethodInfo> _beforeClassMethods = new ();
    private List<MethodInfo> _afterClassMethods = new ();

    private bool _isReady;

    private TestTypeStatus _status = TestTypeStatus.IsRunning;

    public TestType(Type classType)
    {
        TypeOf = classType;

        if (classType.IsAbstract)
        {
            _status = TestTypeStatus.AbstractType;
            IsReady = true;

            return;
        }

        _classInstance = Activator.CreateInstance(classType) ?? throw new InvalidOperationException();
        ParseMethods(classType);
    }

    public IReadOnlyCollection<TestUnit> TestUnitList => _testUnitList.AsReadOnly();

    public int TestMethodsNumber => _testMethods.Count;

    public Type TypeOf { get; }

    public string ExceptionInfo => _exceptions.Count == 0 ?
        string.Empty :
        new AggregateException(_exceptions).ToString();

    public TestTypeStatus Status
    {
        get
        {
            if (_testUnitList.Select(testUnit => testUnit.Status)
                .Any(status =>
                    status is TestUnitStatus.AfterFailed
                        or TestUnitStatus.BeforeFailed
                        or TestUnitStatus.AfterFailed))
            {
                return TestTypeStatus.TestsFailed;
            }

            return _status;
        }
    }

    public int SucceededTestsCount
    {
        get
        {
            Wait();
            return _testUnitList.Select(testUnit => testUnit.Status)
                .Count(status =>
                    status is TestUnitStatus.Succeed
                        or TestUnitStatus.CaughtExpectedException);
        }
    }

    public int SkippedTestsCount
    {
        get
        {
            Wait();
            return _testUnitList.Select(testUnit => testUnit.Status)
                .Count(status =>
                    status is TestUnitStatus.Ignored
                        or TestUnitStatus.MethodHasArguments
                        or TestUnitStatus.NonPublicMethod
                        or TestUnitStatus.NonVoidMethod
                        or TestUnitStatus.StaticMethod);
        }
    }

    public int FailedTestsCount => TestMethodsNumber - SkippedTestsCount - SucceededTestsCount;

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
        if (IsReady)
        {
            return;
        }

        if (!RunBeforeClassMethods())
        {
            IsReady = true;

            return;
        }

        RunTestMethods();
        RunAfterClassMethods();

        IsReady = true;
    }

    public void Wait()
    {
        lock (_locker)
        {
            if (IsReady)
            {
                return;
            }

            Monitor.Wait(_locker);
        }
    }

    public void AcceptPrinter(IPrinter printer)
    {
        Wait();
        printer.PrintTestTypeInfo(this);
    }

    private bool RunBeforeClassMethods()
    {
        foreach (var beforeClassMethod in _beforeClassMethods)
        {
            if (!TryRunBeforeClass(beforeClassMethod))
            {
                return false;
            }
        }

        return true;
    }

    private void RunAfterClassMethods()
    {
        Parallel.ForEach(_afterClassMethods, afterClassMethod =>
        {
            TryRunAfterClass(afterClassMethod);
        });
    }

    private void RunTestMethods()
    {
        Parallel.ForEach(_testMethods, testMethod =>
        {
            var testUnit = new TestUnit(_classInstance, testMethod, _beforeMethods, _afterMethods);
            _testUnitList.Add(testUnit);
            testUnit.Run();
        });
    }

    private bool TryRunBeforeClass(MethodInfo beforeClassMethod)
    {
        if (!CheckMethodSignature(beforeClassMethod))
        {
            return false;
        }

        try
        {
            beforeClassMethod.Invoke(null, null);
            return true;
        }
        catch (TargetInvocationException exception)
        {
            _exceptions.Add(exception);
            SetStatus(TestTypeStatus.BeforeClassFailed);
            return false;
        }
    }

    private bool TryRunAfterClass(MethodInfo afterClassMethod)
    {
        if (!CheckMethodSignature(afterClassMethod))
        {
            return false;
        }

        try
        {
            afterClassMethod.Invoke(null, null);
            return true;
        }
        catch (TargetInvocationException exception)
        {
            _exceptions.Add(exception);
            SetStatus(TestTypeStatus.AfterClassFailed);
            return false;
        }
    }

    private bool CheckMethodSignature(MethodInfo method)
    {
        var methodSignature = new MethodSignature(method);

        var previousStatus = _status;

        _status = methodSignature switch
        {
            { IsPublic: false } => TestTypeStatus.NonPublicMethod,
            { IsStatic: false } => TestTypeStatus.NonStaticMethod,
            { HasArguments: true } => TestTypeStatus.MethodHasArguments,
            { IsVoid: false } => TestTypeStatus.NonVoidMethod,
            _ => _status,
        };

        return _status == previousStatus;
    }

    private void SetStatus(TestTypeStatus status) => _status = status;

    private void ParseMethods(Type classType)
    {
        var methods = classType.GetMethods().ToList();

        _testMethods = GetMethodsWithSpecificAttribute(methods, typeof(TestAttribute));
        _beforeMethods = GetMethodsWithSpecificAttribute(methods, typeof(BeforeAttribute));
        _afterMethods = GetMethodsWithSpecificAttribute(methods, typeof(AfterAttribute));
        _beforeClassMethods = GetMethodsWithSpecificAttribute(methods, typeof(BeforeClassAttribute));
        _afterClassMethods = GetMethodsWithSpecificAttribute(methods, typeof(AfterClassAttribute));
    }

    private List<MethodInfo> GetMethodsWithSpecificAttribute(List<MethodInfo> methods, Type attributeType) =>
        methods.Where(method =>
            Attribute.GetCustomAttributes(method)
                .Select(attr => attr.GetType())
                .Contains(attributeType)).ToList();
}