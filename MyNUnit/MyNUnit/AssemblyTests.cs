namespace MyNUnit;

using System.Reflection;
using Attributes;

public class AssemblyTests : IPrinter
{
    private readonly Assembly _assembly;

    private readonly List<TestClass> _testClassList = new ();

    public AssemblyTests(string path)
    {
        _assembly = Assembly.LoadFrom(path);
    }

    public void RunTests()
    {
        var testTypes = _assembly.ExportedTypes;

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
    }

    public void Print()
    {
        Console.WriteLine(_assembly.GetName());
        foreach (var testClass in _testClassList)
        {
            testClass.Print();
        }
    }
}