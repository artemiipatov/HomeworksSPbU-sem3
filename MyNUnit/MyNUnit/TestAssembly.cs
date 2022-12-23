namespace MyNUnit;

using System.Reflection;
using Attributes;

public class TestAssembly
{
    private readonly List<TestType> _testTypeList = new ();

    private readonly object _locker = new ();

    private bool _isReady;

    public TestAssembly(string path)
    {
        Assembly = Assembly.LoadFrom(path);
    }

    public IReadOnlyCollection<TestType> TestTypeList => _testTypeList.AsReadOnly();

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
        var testTypes = GetTestTypes();

        Parallel.ForEach(testTypes, testType =>
        {
            var testTypeInstance = new TestType(testType);
            _testTypeList.Add(testTypeInstance);
            testTypeInstance.Run();
        });

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

    private List<Type> GetTestTypes() =>
        (from type in Assembly.ExportedTypes
            let methods = type.GetMethods()
            where methods.Any(
                method =>
                    Attribute.GetCustomAttributes(method)
                        .Select(attr => attr.GetType())
                        .Contains(typeof(TestAttribute)))
            select type)
        .ToList();
}