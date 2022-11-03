namespace MatrixMultiplication;

/// <summary>
/// Builds a table for comparing concurrent and serial multiplication statistics.
/// </summary>
public static class TableBuilder
{
    /// <summary>
    /// Creates base structure of a table, which should contain expected values and standard deviations of serial and concurrent matrix multiplications.
    /// </summary>
    /// <param name="path">Path to the file which should contain table.</param>
    public static void CreateTable(string path)
    {
        using var file = new StreamWriter(File.Create(path));

        file.WriteLine("|   size  |         Concurrent Multiplication         |            Serial Multiplication          |");
        file.WriteLine("|---------|------------------|------------------------|------------------|------------------------|");
        file.WriteLine("|         |Expected Value, ms| Standard Deviation, ms |Expected Value, ms| Standard Deviation, ms |");
        file.WriteLine("|---------|------------------|------------------------|------------------|------------------------|");

        file.Close();
    }

    /// <summary>
    /// Adds new string to the table, filled with the size of the current matrices, expected values and standard deviations of concurrent and serial multiplications.
    /// </summary>
    /// <param name="path">Path to the file with table.</param>
    /// <param name="size">Size of matrices.</param>
    /// <param name="expectedValue1">Expected value of the concurrent multiplications.</param>
    /// <param name="deviation1">Standard deviation of the concurrent multiplications.</param>
    /// <param name="expectedValue2">Expected value of the serial multiplications.</param>
    /// <param name="deviation2">Standard deviation of the serial multiplications.</param>
    public static void WriteDataToTable(string path, (int, int) size, double expectedValue1, double deviation1, double expectedValue2, double deviation2)
    {
        using var file = new StreamWriter(File.OpenWrite(path));
        file.BaseStream.Seek(0, SeekOrigin.End);

        var spacesAfterExpectedValue1 = new string(' ', 18 - expectedValue1.ToString().Length);
        var spacesAfterDeviation1 = new string(' ', 24 - deviation1.ToString().Length);
        var spacesAfterExpectedValue2 = new string(' ', 18 - expectedValue2.ToString().Length);
        var spacesAfterDeviation2 = new string(' ', 24 - deviation2.ToString().Length);

        file.WriteLine($"| {size.Item1}x{size.Item2} |{expectedValue1}{spacesAfterExpectedValue1}|{deviation1}{spacesAfterDeviation1}|" +
                       $"{expectedValue2}{spacesAfterExpectedValue2}|{deviation2}{spacesAfterDeviation2}|");
        file.Close();
    }
}