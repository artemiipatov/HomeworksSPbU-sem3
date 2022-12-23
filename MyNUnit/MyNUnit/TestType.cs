using System.Collections.Concurrent;

namespace MyNUnit;

using System.Reflection;
using Attributes;

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

    public TestType(Type classType)
    {
        _classInstance = Activator.CreateInstance(classType) ?? throw new InvalidOperationException();
        ParseMethods(classType);
    }

    public IReadOnlyCollection<TestUnit> TestUnitList => _testUnitList.AsReadOnly();

    public int TestMethodsNumber => _testMethods.Count;

    public Type TypeOf => _classInstance.GetType();

    public string ExceptionInfo => new AggregateException(_exceptions).ToString();

    public Status BeforeClassStatus { get; private set; } = Status.IsRunning;

    public Status AfterClassStatus { get; private set; } = Status.IsRunning;

    public Status GeneralStatus
    {
        get
        {
            if (BeforeClassStatus == Status.IsRunning
                || AfterClassStatus == Status.IsRunning
                || _testUnitList.Select(testUnit => testUnit.GeneralStatus).Contains(Status.IsRunning))
            {
                return Status.IsRunning;
            }

            if (BeforeClassStatus != Status.Succeed
                || AfterClassStatus != Status.Succeed
                || _testUnitList.Select(testUnit => testUnit.GeneralStatus).Any(status => status != Status.Succeed))
            {
                return Status.Failed;
            }

            return Status.Succeed;
        }
    }

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
        if (!RunBeforeClassMethods())
        {
            IsReady = true;
            return;
        }

        RunTestMethods();

        RunAfterClassMethods();

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

        printer.PrintTestTypeInfo(this);
    }

    private bool RunBeforeClassMethods()
    {
        foreach (var beforeClassMethod in _beforeClassMethods)
        {
            BeforeClassStatus = CheckMethodSignature(beforeClassMethod, BeforeClassStatus);
            if (BeforeClassStatus != Status.IsRunning
                || !TryRunBeforeClass(beforeClassMethod))
            {
                return false;
            }
        }

        BeforeClassStatus = Status.Succeed;

        return true;
    }

    private bool RunAfterClassMethods()
    {
        foreach (var afterClassMethod in _afterClassMethods)
        {
            AfterClassStatus = CheckMethodSignature(afterClassMethod, AfterClassStatus);
            if (AfterClassStatus != Status.IsRunning
                || !TryRunAfterClass(afterClassMethod))
            {
                return false;
            }
        }

        AfterClassStatus = Status.Succeed;

        return true;
    }

    private void RunTestMethods()
    {
        Parallel.ForEach(_testMethods, testMethod =>
        {
            var testUnit = new TestUnit(_classInstance, testMethod, _beforeMethods, _afterMethods);
            _testUnitList.Add(testUnit);
        });
    }

    private bool TryRunBeforeClass(MethodInfo beforeClassMethod)
    {
        try
        {
            beforeClassMethod.Invoke(null, null);
            return true;
        }
        catch (TargetInvocationException exception)
        {
            _exceptions.Add(exception);
            BeforeClassStatus = Status.Failed;
            return false;
        }
    }

    private bool TryRunAfterClass(MethodInfo afterClassMethod)
    {
        try
        {
            afterClassMethod.Invoke(null, null);
            return true;
        }
        catch (TargetInvocationException exception)
        {
            _exceptions.Add(exception);
            AfterClassStatus = Status.Failed;
            return false;
        }
    }

    private void ParseMethods(Type classType)
    {
        var methods = classType.GetMethods(BindingFlags.Static).ToList();

        _testMethods = GetMethodsWithSpecificAttribute(methods, typeof(TestAttribute));
        _beforeMethods = GetMethodsWithSpecificAttribute(methods, typeof(BeforeAttribute));
        _afterMethods = GetMethodsWithSpecificAttribute(methods, typeof(AfterAttribute));
        _beforeClassMethods = GetMethodsWithSpecificAttribute(methods, typeof(BeforeClassAttribute));
        _afterClassMethods = GetMethodsWithSpecificAttribute(methods, typeof(AfterClassAttribute));
    }

    private Status CheckMethodSignature(MethodInfo method, Status status)
    {
        var methodSignature = new MethodSignature(method);
        return methodSignature switch
        {
            { IsPublic: false } => Status.NonPublicMethod,
            { IsStatic: false } => Status.NonStaticMethod,
            { HasArguments: true } => Status.MethodHasArguments,
            { IsVoid: false } => Status.NonVoidMethod,
            _ => status,
        };
    }

    private List<MethodInfo> GetMethodsWithSpecificAttribute(List<MethodInfo> methods, Type attributeType) =>
        methods.Where(method =>
            Attribute.GetCustomAttributes(method)
                .Select(attr => attr.GetType())
                .Contains(attributeType)).ToList();
}