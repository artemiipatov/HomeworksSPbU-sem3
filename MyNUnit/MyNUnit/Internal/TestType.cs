namespace MyNUnit.Internal;

using System.Collections.Concurrent;
using System.Reflection;
using Attributes;
using Printer;

/// <summary>
/// Abstraction for creating instance of test type, running BeforeClass, AfterClass methods and test units withing current instance.
/// </summary>
public class TestType
{
    private readonly ConcurrentQueue<TestUnit> _testUnitCollection = new ();

    private readonly object _locker = new ();

    private BlockingCollection<Exception> _exceptions = new ();

    private List<MethodInfo> _testMethods = new ();
    private List<MethodInfo> _beforeMethods = new ();
    private List<MethodInfo> _afterMethods = new ();
    private List<MethodInfo> _beforeClassMethods = new ();
    private List<MethodInfo> _afterClassMethods = new ();

    private bool _isReady;

    private TestTypeStatus _status = TestTypeStatus.IsRunning;

    /// <summary>
    /// Initializes a new instance of the <see cref="TestType"/> class.
    /// </summary>
    /// <param name="classType">Type of class with tests.</param>
    /// <exception cref="InvalidOperationException">Throws if instance of the current type cannot be created.</exception>
    public TestType(Type classType)
    {
        TypeOf = classType;

        if (classType.IsAbstract)
        {
            SetStatus(TestTypeStatus.AbstractType);
            IsReady = true;

            return;
        }

        ParseMethods(classType);
    }

    /// <summary>
    /// Gets read only collection of <see cref="TestUnit"/> of current test type.
    /// </summary>
    public IReadOnlyCollection<TestUnit> TestUnitCollection => _testUnitCollection;

    /// <summary>
    /// Gets number of <see cref="TestAttribute"/> methods.
    /// </summary>
    public int TestMethodsNumber => _testMethods.Count;

    public Type TypeOf { get; }

    /// <summary>
    /// Gets information of caught unexpected exceptions.
    /// If no unexpected exceptions was caught, returns empty string.
    /// </summary>
    public string ExceptionInfo => _exceptions.Count == 0 ?
        string.Empty :
        new AggregateException(_exceptions).ToString();

    /// <summary>
    /// Gets status of test type run.
    /// </summary>
    public TestTypeStatus Status
    {
        get
        {
            if (_testUnitCollection.Select(testUnit => testUnit.Status)
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

    /// <summary>
    /// Gets number of succeeded tests.
    /// </summary>
    public int SucceededTestsCount
    {
        get
        {
            Wait();
            return _testUnitCollection.Select(testUnit => testUnit.Status)
                .Count(status =>
                    status is TestUnitStatus.Succeed
                        or TestUnitStatus.CaughtExpectedException);
        }
    }

    /// <summary>
    /// Gets number of skipped tests.
    /// </summary>
    public int SkippedTestsCount
    {
        get
        {
            Wait();
            return _testUnitCollection.Select(testUnit => testUnit.Status)
                .Count(status =>
                    status is TestUnitStatus.Ignored
                        or TestUnitStatus.MethodHasArguments
                        or TestUnitStatus.NonPublicMethod
                        or TestUnitStatus.NonVoidMethod
                        or TestUnitStatus.StaticMethod);
        }
    }

    /// <summary>
    /// Gets number of failed tests.
    /// </summary>
    public int FailedTestsCount => TestMethodsNumber - SkippedTestsCount - SucceededTestsCount;

    /// <summary>
    /// Gets a value indicating whether all tests finished.
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
    /// Runs tests from test type.
    /// </summary>
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

        if (Status == TestTypeStatus.IsRunning)
        {
            _status = TestTypeStatus.Succeed;
        }

        IsReady = true;
    }

    /// <summary>
    /// Blocks current thread until all tests are finished.
    /// </summary>
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

    /// <summary>
    /// Accepts <see cref="IPrinter"/> instance.
    /// </summary>
    /// <param name="printer">Objects that implements <see cref="IPrinter"/>.</param>
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
            var classInstance = Activator.CreateInstance(TypeOf) ?? throw new InvalidOperationException();
            var testUnit = new TestUnit(classInstance, testMethod, _beforeMethods, _afterMethods);
            _testUnitCollection.Enqueue(testUnit);
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

    private void TryRunAfterClass(MethodInfo afterClassMethod)
    {
        if (!CheckMethodSignature(afterClassMethod))
        {
            return;
        }

        try
        {
            afterClassMethod.Invoke(null, null);
        }
        catch (TargetInvocationException exception)
        {
            _exceptions.Add(exception);
            SetStatus(TestTypeStatus.AfterClassFailed);
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