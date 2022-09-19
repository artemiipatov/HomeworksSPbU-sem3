namespace MatrixMultiplication;

using System.Linq;

public class DenseMatrix
{
    /// <summary>
    ///  Matrix constructor, which takes as an argument path to the file with matrix and builds 2d array using matrix from file.
    /// </summary>
    /// <param name="path">Path to the file with matrix.</param>
    public DenseMatrix(string path)
    {
        _matrix = ReadFile(path);
        NumberOfRows = _matrix.GetLength(0);
        NumberOfColumns = _matrix.GetLength(1);
        // Path = path;
    }

    /// <summary>
    /// Matrix constructor, which takes 2d array and writes it to the file with given path.
    /// </summary>
    /// <param name="matrix">2d array, which represents matrix.</param>
    /// <param name="path">Path to the file, which will contain given matrix.</param>
    public DenseMatrix(int[,] matrix)
    {
        _matrix = matrix;
        NumberOfRows = _matrix.GetLength(0);
        NumberOfColumns = _matrix.GetLength(1);
        // WriteMatrixToFile(matrix, path);
        // Path = path;
    }

    /// <summary>
    /// Path to the file with matrix.
    /// </summary>
    // public readonly string Path;
    
    private readonly int[,] _matrix;
    
    /// <summary>
    /// 2d integer array which represents matrix.
    /// </summary>
    public int[,] Matrix => _matrix;

    /// <summary>
    /// Rows in matrix.
    /// </summary>
    public int NumberOfRows { get; }
    
    /// <summary>
    /// Returns number of columns in matrix.
    /// </summary>
    public int NumberOfColumns { get; }
    
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

    private static void WriteMatrixToFile(int[,] matrix, string path)
    {
        using var file = new StreamWriter(File.Create(path));
        for (var row = 0; row < matrix.GetLength(0); row++)
        {
            for (var column = 0; column < matrix.GetLength(1); column++)
            {
                file.Write(matrix[row, column] + (column == matrix.GetLength(1) - 1 ? (row < matrix.GetLength(0) - 1 ? "\n" : "") : " "));
            }
        }
        
        file.Close();
    }
    
    /// <summary>
    /// Multiplies matrix using several threads. Number of threads depends on the number of system's processors.
    /// </summary>
    /// <param name="matrix1">Left matrix.</param>
    /// <param name="matrix2">Right matrix.</param>
    /// <returns>Result of matrix multiplication.</returns>
    public static DenseMatrix MultiplyConcurrently(DenseMatrix matrix1, DenseMatrix matrix2)
    {
        var numberOfThreads = Environment.ProcessorCount / 2 <= Math.Max(matrix1.NumberOfRows, matrix2.NumberOfColumns) ? 
            Environment.ProcessorCount / 2 : Math.Max(matrix1.NumberOfRows, matrix2.NumberOfColumns);
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
            var initialRow = rowsThreading ? i * rowsRestriction : 0;
            var initialColumn = rowsThreading ? 0 : i * columnsRestriction;
            var lastRow = rowsThreading ? (i == numberOfThreads - 1 ? numberOfRows : (i + 1) * rowsRestriction) : numberOfRows;
            var lastColumn = rowsThreading ? numberOfColumns : (i == numberOfThreads - 1 ? numberOfColumns : (i + 1) * columnsRestriction);

            threads[i] = new Thread(() =>
            {
                for (var row = initialRow; row < lastRow; row++)
                {
                    for (var column = initialColumn; column < lastColumn; column++)
                    {
                        result[row, column] = DotProduct(matrix1, matrix2, row, column);
                    }
                }
            });
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

    private static int DotProduct(DenseMatrix matrix1, DenseMatrix matrix2, int row, int col)
    {
        var result = 0;
        for (var i = 0; i < matrix1.NumberOfColumns; i++)
        {
            result += matrix1.Matrix[row, i] * matrix2.Matrix[i, col];
        }

        return result;
    }

    /// <summary>
    /// Builds a matrix with given size and put it into the file with given path.
    /// </summary>
    /// <param name="path">Path to the file, which will contain matrix.</param>
    /// <param name="size">Size of matrix.</param>
    public static void MatrixGenerator(string path, (int, int) size)
    {
        using var file = new StreamWriter(File.Create(path));

        var rnd = new Random();

        for (var row = 0; row < size.Item1; row++)
        {
            for (var column = 0; column < size.Item2; column++)
            {
                file.Write(rnd.Next(0, 3) + (column == size.Item2 - 1 ? "" : " "));
            }

            if (row < size.Item1 - 1)
            {
                file.WriteLine();
            }
        }
    }
}
