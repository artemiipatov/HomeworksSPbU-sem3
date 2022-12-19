namespace MyNUnit;

public class MyNUnit : IPrinter
{
    private readonly List<AssemblyTests> _assemblyTestsList = new ();

    public void RunTestsFromAllAssemblies(string[] paths)
    {
        foreach (var path in paths)
        {
            var assemblyTests = new AssemblyTests(path);
            _assemblyTestsList.Add(assemblyTests);
            Task.Run(assemblyTests.RunTests);
        }
    }

    public void Print()
    {
        Console.WriteLine("MyNUnit");
        foreach (var assemblyTests in _assemblyTestsList)
        {
            assemblyTests.Print();
        }
    }
}