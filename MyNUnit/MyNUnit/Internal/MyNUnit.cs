namespace MyNUnit.Internal;

using Printer;

/// <summary>
/// Class that contains assemblies for testing.
/// Provides method for running tests from all assemblies.
/// </summary>
public class MyNUnit
{
    private readonly List<TestAssembly> _assemblyTestsList = new ();

    private readonly object _locker = new ();

    private bool _isReady;

    /// <summary>
    /// Gets read only collection of <see cref="TestAssembly"/>.
    /// </summary>
    public IReadOnlyCollection<TestAssembly> TestAssemblyList =>
        _assemblyTestsList.AsReadOnly();

    /// <summary>
    /// Gets a value indicating whether execution of tests from all assemblies has been completed.
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
    /// Runs tests from all given assemblies.
    /// </summary>
    /// <param name="paths">Array of paths to assemblies.
    /// Path can be either to file or to directory contains assemblies.
    /// </param>
    /// <exception cref="FileNotFoundException">
    /// Throws when file or directory with given path does not exist.
    /// </exception>
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

    /// <summary>
    /// Accepts printer for printing information about current <see cref="MyNUnit"/> class.
    /// </summary>
    /// <param name="printer">Instance of current printer class.</param>
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