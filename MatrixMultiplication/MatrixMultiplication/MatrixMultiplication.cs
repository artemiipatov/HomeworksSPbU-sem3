namespace MatrixMultiplication;

using System.Linq;

/// <summary>
/// Class that wraps 2d array and represents matrix. It also contains concurrent and serial multiplications.
/// </summary>
public class DenseMatrix
{
    private static readonly Random Rnd = new ();

    private readonly int[,] _matrix;

    /// <summary>
    /// Initializes a new instance of the <see cref="DenseMatrix"/> class.
    /// </summary>
    /// <param name="path">Path to the file with matrix.</param>
    public DenseMatrix(string path)
    {
        _matrix = ReadFile(path);
        NumberOfRows = _matrix.GetLength(0);
        NumberOfColumns = _matrix.GetLength(1);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DenseMatrix"/> class.
    /// </summary>
    /// <param name="matrix">2d array, which represents matrix.</param>
    public DenseMatrix(int[,] matrix)
    {
        _matrix = matrix;
        NumberOfRows = _matrix.GetLength(0);
        NumberOfColumns = _matrix.GetLength(1);
    }

    /// <summary>
    /// Gets 2d integer array which represents matrix.
    /// </summary>
    public int[,] Matrix => _matrix;

    /// <summary>
    /// Gets number of rows in matrix.
    /// </summary>
    public int NumberOfRows { get; }

    /// <summary>
    /// Gets number of columns in matrix.
    /// </summary>
    public int NumberOfColumns { get; }

    /// <summary>
    /// Multiplies matrix using several threads. Number of threads depends on the number of system's processors.
    /// </summary>
    /// <param name="matrix1">Left matrix.</param>
    /// <param name="matrix2">Right matrix.</param>
    /// <returns>Result of matrix multiplication.</returns>
    public static DenseMatrix MultiplyConcurrently(DenseMatrix matrix1, DenseMatrix matrix2)
    {
        var numberOfThreads = Environment.ProcessorCount / 2 <= Math.Max(matrix1.NumberOfRows, matrix2.NumberOfColumns) ?
            Environment.ProcessorCount / 2
            : Math.Max(matrix1.NumberOfRows, matrix2.NumberOfColumns);
        var numberOfRows = matrix1.NumberOfRows;
        var numberOfColumns = matrix2.NumberOfColumns;
        var result = new int[numberOfRows, numberOfColumns];

        var rowsThreading = numberOfRows > numberOfColumns;
        var rowsRestriction = numberOfRows;
        var columnsRestriction = numberOfColumns;

        if (rowsThreading)
        {
            rowsRestriction = numberOfRows / numberOfThreads;
        }
        else
        {
            columnsRestriction = numberOfColumns / numberOfThreads;
        }

        var threads = new Thread[numberOfThreads];

        for (int i = 0; i < numberOfThreads; i++)
        {
            var initialRow = rowsThreading ?
                i * rowsRestriction
                : 0;

            var initialColumn = rowsThreading ?
                0
                : i * columnsRestriction;

            var lastRow = rowsThreading ?
                (i == numberOfThreads - 1 ?
                    numberOfRows
                    : (i + 1) * rowsRestriction)
                : numberOfRows;

            var lastColumn = rowsThreading ?
                numberOfColumns
                : (i == numberOfThreads - 1 ?
                    numberOfColumns
                    : (i + 1) * columnsRestriction);

            threads[i] = new Thread(() => ThreadAction(initialRow, initialColumn, lastRow, lastColumn, matrix1, matrix2, result));
        }

        foreach (var thread in threads)
        {
            thread.Start();
        }

        foreach (var thread in threads)
        {
            thread.Join();
        }

        return new DenseMatrix(result);
    }

    /// <summary>
    /// Multiplies matrix using one thread.
    /// </summary>
    /// <param name="matrix1">Left matrix.</param>
    /// <param name="matrix2">Right matrix.</param>
    /// <returns>Result of matrix multiplication.</returns>
    public static DenseMatrix MultiplySerially(DenseMatrix matrix1, DenseMatrix matrix2)
    {
        var numberOfRows = matrix1.NumberOfRows;
        var numberOfColumns = matrix2.NumberOfColumns;
        var result = new int[numberOfRows, numberOfColumns];

        for (var row = 0; row < numberOfRows; row++)
        {
            for (var column = 0; column < numberOfColumns; column++)
            {
                result[row, column] = DotProduct(matrix1, matrix2, row, column);
            }
        }

        return new DenseMatrix(result);
    }

    /// <summary>
    /// Builds a matrix with given size and put it into the file with given path.
    /// </summary>
    /// <param name="path">Path to the file, which will contain matrix.</param>
    /// <param name="size">Size of matrix.</param>
    public static void MatrixGenerator(string path, (int, int) size)
    {
        using var file = new StreamWriter(File.Create(path));

        for (var row = 0; row < size.Item1; row++)
        {
            for (var column = 0; column < size.Item2; column++)
            {
                file.Write(Rnd.Next(0, 3) + (column == size.Item2 - 1 ? string.Empty : " "));
            }

            if (row < size.Item1 - 1)
            {
                file.WriteLine();
            }
        }
    }

    private static void ThreadAction(
        int initialRow,
        int initialColumn,
        int lastRow,
        int lastColumn,
        DenseMatrix matrix1,
        DenseMatrix matrix2,
        int[,] result)
    {
        for (var row = initialRow; row < lastRow; row++)
        {
            for (var column = initialColumn; column < lastColumn; column++)
            {
                result[row, column] = DotProduct(matrix1, matrix2, row, column);
            }
        }
    }

    private static int[,] ReadFile(string inputFilePath)
    {
        var fileEnumerable = File.ReadLines(inputFilePath);

        var numberOfRows = fileEnumerable.Count();
        var fileEnumerator = fileEnumerable.GetEnumerator();
        fileEnumerator.MoveNext();

        var numberOfColumns = fileEnumerator.Current.Split().Length;
        var matrix = new int[numberOfRows, numberOfColumns];

        for (var row = 0; row < numberOfRows; row++)
        {
            var line = Array.ConvertAll(fileEnumerator.Current.Split(), int.Parse);
            for (var column = 0; column < numberOfColumns; column++)
            {
                matrix[row, column] = line[column];
            }

            fileEnumerator.MoveNext();
        }

        fileEnumerator.Dispose();
        return matrix;
    }

    private static int DotProduct(DenseMatrix matrix1, DenseMatrix matrix2, int row, int col)
    {
        var result = 0;
        for (var i = 0; i < matrix1.NumberOfColumns; i++)
        {
            result += matrix1.Matrix[row, i] * matrix2.Matrix[i, col];
        }

        return result;
    }
}
