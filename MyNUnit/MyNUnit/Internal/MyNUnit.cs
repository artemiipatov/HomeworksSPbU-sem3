namespace MyNUnit.Internal;

using Printer;

public class MyNUnit
{
    private readonly List<TestAssembly> _assemblyTestsList = new ();

    private readonly object _locker = new ();

    private bool _isReady;

    public IReadOnlyCollection<TestAssembly> TestAssemblyList => _assemblyTestsList.AsReadOnly();

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

    public void Run(string[] paths)
    {
        Parallel.ForEach(paths, path =>
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

        printer.PrintMyNUnitInfo(this);
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
        var assemblyTests = new TestAssembly(path);
        _assemblyTestsList.Add(assemblyTests);
        Task.Run(assemblyTests.Run);
    }
}