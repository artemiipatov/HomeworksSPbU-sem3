namespace MyNUnit.Internal;

using System.Reflection;
using Attributes;
using Printer;

/// <summary>
/// Abstraction for running test types from the assembly.
/// </summary>
public class TestAssembly
{
    private readonly List<TestType> _testTypeList = new ();

    private readonly object _locker = new ();

    private bool _isReady;

    /// <summary>
    /// Initializes a new instance of the <see cref="TestAssembly"/> class.
    /// </summary>
    /// <param name="path">Path to assembly.</param>
    public TestAssembly(string path)
    {
        Assembly = Assembly.LoadFrom(path);
    }

    /// <summary>
    /// Gets read only collection of <see cref="TestType"/>.
    /// </summary>
    public IReadOnlyCollection<TestType> TestTypeList => _testTypeList.AsReadOnly();

    /// <summary>
    /// Assembly with test types.
    /// </summary>
    public Assembly Assembly { get; }

    /// <summary>
    /// Gets status of current test assembly run.
    /// </summary>
    public TestAssemblyStatus Status => FailedTestsCount > 0
        ? TestAssemblyStatus.Failed
        : TestAssemblyStatus.Succeed;

    /// <summary>
    /// Gets number of failed tests in the assembly.
    /// </summary>
    public int FailedTestsCount =>
        _testTypeList.Select(testType => testType.FailedTestsCount).Sum();

    /// <summary>
    /// Gets number of skipped tests in the assembly.
    /// </summary>
    public int SkippedTestsCount =>
        _testTypeList.Select(testType => testType.SkippedTestsCount).Sum();

    /// <summary>
    /// Gets number of succeeded tests in the assembly.
    /// </summary>
    public int SucceededTestsCount =>
        _testTypeList.Select(testType => testType.SucceededTestsCount).Sum();

    /// <summary>
    /// Gets a value indicating whether all tests from the assembly are finished.
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
    /// Runs tests from assembly.
    /// </summary>
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