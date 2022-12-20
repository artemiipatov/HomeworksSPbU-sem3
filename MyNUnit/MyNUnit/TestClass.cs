namespace MyNUnit;

using System.Reflection;
using Attributes;

public class TestClass
{
    private readonly object _classInstance;

    private readonly List<TestUnit> _testUnitList = new ();
    private readonly List<Task> _taskList = new ();

    private readonly object _locker = new ();

    private List<MethodInfo> _testMethods = new ();
    private List<MethodInfo> _beforeMethods = new ();
    private List<MethodInfo> _afterMethods = new ();
    private List<MethodInfo> _beforeClassMethods = new ();
    private List<MethodInfo> _afterClassMethods = new ();

    private bool _isReady;

    public TestClass(Type classType)
    {
        _classInstance = Activator.CreateInstance(classType) ?? throw new InvalidOperationException();
        ParseMethods(classType);
    }

    public IReadOnlyCollection<TestUnit> TestUnitList => _testUnitList.AsReadOnly();

    public Type ClassType => _classInstance.GetType();

    public string BeforeClassExceptionInfo { get; private set; } = string.Empty;

    public string AfterClassExceptionInfo { get; private set; } = string.Empty;

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

    public void RunTests()
    {
        try
        {
            RunBeforeClass();
        }
        catch (Exception exception)
        {
            BeforeClassExceptionInfo = exception.ToString();
        }

        foreach (var testMethod in _testMethods)
        {
            var testUnit = new TestUnit(_classInstance, testMethod, _beforeMethods, _afterMethods);
            _testUnitList.Add(testUnit);
            _taskList.Add(Task.Run(testUnit.RunTest));
        }

        Task.WaitAll(_taskList.ToArray());

        try
        {
            RunAfterClass();
        }
        catch (Exception exception)
        {
            AfterClassExceptionInfo = exception.ToString();
        }

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

        printer.PrintTestClassInfo(this);
    }

    private void RunBeforeClass()
    {
        foreach (var beforeClass in _beforeClassMethods)
        {
            beforeClass.Invoke(_classInstance, null);
        }
    }

    private void RunAfterClass()
    {
        foreach (var afterClass in _afterClassMethods)
        {
            afterClass.Invoke(_classInstance, null);
        }
    }

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