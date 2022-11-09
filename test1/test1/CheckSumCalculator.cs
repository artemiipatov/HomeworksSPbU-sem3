namespace test1;

using System.Security.Cryptography;

/// <summary>
/// Calculates check sum of specific directory or file.
/// </summary>
public static class CheckSumCalculator
{
    /// <summary>
    /// Calculates check sum in one thead mode.
    /// </summary>
    /// <param name="path">Path of file or directory.</param>
    /// <returns>Check sum.</returns>
    /// <exception cref="FileNotFoundException">Throws if path does not point to file or directory.</exception>
    public static byte[] CalculateCheckSumSerially(string path)
    {
        if (File.Exists(path))
        {
            return CalculateFileCheckSum(path);
        }
        if (Directory.Exists(path))
        {
            return CalculateDirectoryHashSerially(path);
        }
        
        throw new FileNotFoundException("Invalid path.");
    }

    /// <summary>
    /// Calculates check sum using multiple threads.
    /// </summary>
    /// <param name="path">Path of file or directory.</param>
    /// <returns>Check sum.</returns>
    /// <exception cref="FileNotFoundException">Throws if path does not point to file or directory.</exception>
    public static byte[] CalculateCheckSumConcurrently(string path)
    {
        if (File.Exists(path))
        {
            return CalculateFileCheckSum(path);
        }
        if (Directory.Exists(path))
        {
            return CalculateDirectoryHashConcurrently(path);
        }
        
        throw new FileNotFoundException("Invalid path.");
    }

    /// <summary>
    /// Calculates check sum of specific file.
    /// </summary>
    /// <param name="path">Path of the file.</param>
    /// <returns>Check sum of the file.</returns>
    private static byte[] CalculateFileCheckSum(string path)
    {
        using var file = File.OpenRead(path);
        using var md5 = MD5.Create();

        return md5.ComputeHash(file);
    }

    private static string[] GetSortedItems(string path)
    {
        var files = Directory.GetFiles(path);
        var directories = Directory.GetDirectories(path);
        var items = files.Concat(directories).ToArray();
        Array.Sort(items);

        return items;
    }

    private static byte[] CalculateDirectoryHashSerially(string path)
    {
        var items = GetSortedItems(path);
        var hashes = new List<byte>();

        foreach (var item in items)
        {
            if (File.Exists(item))
            {
                CalculateFileCheckSum(item).ToList().ForEach(x => hashes.Add(x));
            }
            else
            {
                CalculateDirectoryHashSerially(item).ToList().ForEach(x => hashes.Add(x));
            }
        }

        using var md5 = MD5.Create();

        return md5.ComputeHash(hashes.ToArray());
    }

    private static byte[] CalculateDirectoryHashConcurrently(string path)
    {
        var items = GetSortedItems(path);
        var hashes = new SortedDictionary<string, byte[]>();
        var numberOfThreads = Environment.ProcessorCount / 2 < 2 ? 2 : Environment.ProcessorCount / 2;
        var counter = 0;
        var locker = new object();

        for (var i = 0; i < numberOfThreads; i++)
        {
            var startingIndex = i;
            Task.Run(() =>
            {
                var localItems = Array.Empty<string>();

                for (var j = startingIndex;
                     j < (startingIndex == numberOfThreads - 1
                         ? items.Length
                         : (startingIndex + 1) * items.Length / numberOfThreads);
                     j++)
                {
                    localItems[j - startingIndex] = items[j];
                }

                foreach (var item in localItems)
                {
                    if (File.Exists(item))
                    {
                        var hash = CalculateFileCheckSum(item);
                        lock (hashes)
                        {
                            hashes.Add(item, hash);
                        }
                    }

                    else
                    {
                        var hash = CalculateDirectoryHashSerially(item);
                        lock (hashes)
                        {
                            hashes.Add(item, hash);
                        }
                    }
                }

                Interlocked.Increment(ref counter);
                if (counter == numberOfThreads)
                {
                    Monitor.Pulse(locker);
                }
            });
        }

        while (counter != numberOfThreads)
        {
            Monitor.Wait(locker);
        }

        var hashesList = hashes.Values.SelectMany(value => value).ToList();
        using var md5 = MD5.Create();

        return md5.ComputeHash(hashesList.ToArray());
    }
}