namespace MyNUnit;

public class MyNUnit : IPrinter
{
    private readonly List<AssemblyTests> _assemblyTestsList = new ();

    public void RunTestsFromAllAssemblies(string[] paths)
    {
        foreach (var path in paths)
        {
            if (File.Exists(path))
            {
                LoadAssembly(path);
            }
            else if (Directory.Exists(path))
            {
                LoadAllAssemblies(path);
            }
            else
            {
                throw new FileNotFoundException("Non existent file or directory path.");
            }
        }
    }

    private void LoadAllAssemblies(string directoryPath)
    {
        var assemblies = Directory.EnumerateFiles(directoryPath, "*.dll");
        foreach (var dllPath in assemblies)
        {
            LoadAssembly(dllPath);
        }
    }

    private void LoadAssembly(string path)
    {
        var assemblyTests = new AssemblyTests(path);
        _assemblyTestsList.Add(assemblyTests);
        Task.Run(assemblyTests.RunTests);
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