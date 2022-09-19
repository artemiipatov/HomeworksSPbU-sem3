using System.Diagnostics;
using MatrixMultiplication;
using Matrix = MatrixMultiplication.DenseMatrix;

const int n = 10;

Table.CreateTable("table.txt");

for (var j = 1; j < 9; j++)
{
    var size = j * 100;
    Matrix.MatrixGenerator("matrix1.txt", (size, size));
    Matrix.MatrixGenerator("matrix2.txt", (size, size));
    var matrix1 = new Matrix("matrix1.txt");
    var matrix2 = new Matrix("matrix2.txt");
    var calculationTime = new long[n];
    
    for (var i = 0; i < n; i++)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        Matrix.MultiplyConcurrently(matrix1, matrix2);
        stopwatch.Stop();
        calculationTime[i] = stopwatch.ElapsedMilliseconds;
    }

    var expectedValue1 = Comparison.CalculateExpectedValue(calculationTime);
    var deviation1 = Comparison.CalculateDeviation(calculationTime);

    for (var i = 0; i < n; i++)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        Matrix.MultiplySerially(matrix1, matrix2);
        stopwatch.Stop();
        calculationTime[i] = stopwatch.ElapsedMilliseconds;
    }
    
    var expectedValue2 = Comparison.CalculateExpectedValue(calculationTime);
    var deviation2 = Comparison.CalculateDeviation(calculationTime);

    Table.WriteDataToTable("table.txt", (size, size), expectedValue1, deviation1, expectedValue2, deviation2);
}