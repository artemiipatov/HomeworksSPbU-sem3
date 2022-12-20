namespace MyNUnit;

using System.Reflection;
using Attributes;

public class TestAssembly
{
    private readonly List<TestClass> _testClassList = new ();

    private readonly object _locker = new ();

    private bool _isReady;

    public TestAssembly(string path)
    {
        Assembly = Assembly.LoadFrom(path);
    }

    public IReadOnlyCollection<TestClass> TestClassList => _testClassList.AsReadOnly();

    public Assembly Assembly { get; }

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
        var testTypes = Assembly.ExportedTypes;

        var testClassTypes = (
            from type in testTypes
            let methods = type.GetMethods()
            where methods.Any(
                method =>
                    Attribute.GetCustomAttributes(method)
                    .Select(attr => attr.GetType())
                    .Contains(typeof(TestAttribute)))
            select type)
            .ToList();

        foreach (var testClassType in testClassTypes)
        {
            var testClassInstance = new TestClass(testClassType);
            _testClassList.Add(testClassInstance);
            Task.Run(testClassInstance.RunTests);
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

        printer.PrintAssemblyInfo(this);
    }
}