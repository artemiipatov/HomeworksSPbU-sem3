namespace MyNUnit;

using System.Reflection;
using Attributes;

public class TestClass : IPrinter
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

    private string _beforeClassExceptionInfo = string.Empty;
    private string _afterClassExceptionInfo = string.Empty;

    private bool _isReady;

    public TestClass(Type classType)
    {
        _classInstance = Activator.CreateInstance(classType) ?? throw new InvalidOperationException();
        ParseMethods(classType);
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

    public void RunTests()
    {
        try
        {
            RunBeforeClass();
        }
        catch (Exception exception)
        {
            _beforeClassExceptionInfo = exception.GetType()
                                        + exception.Message
                                        + (exception.StackTrace ?? string.Empty);
        }

        foreach (var testMethod in _testMethods)
        {
            var testUnit = new TestUnit(this, testMethod, _beforeMethods, _afterMethods);
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
            _afterClassExceptionInfo = exception.GetType()
                                       + exception.Message
                                       + (exception.StackTrace ?? string.Empty);
        }
    }

    public void Print()
    {
        if (_beforeClassExceptionInfo.Length != 0)
        {
            Console.WriteLine(_beforeClassExceptionInfo);
        }

        Console.WriteLine(_classInstance.GetType());

        foreach (var testUnit in _testUnitList)
        {
            testUnit.Print();
        }

        if (_afterClassExceptionInfo.Length != 0)
        {
            Console.WriteLine(_afterClassExceptionInfo);
        }
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