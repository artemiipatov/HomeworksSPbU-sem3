namespace MatrixMultiplication.Tests;

using System.IO;
using NUnit.Framework;

public class Tests
{
    [Test]
    public void FileIsReadCorrectly()
    {
        using var file1 = new StreamWriter(File.Create("matrix1.txt"));
        using var file2 = new StreamWriter(File.Create("matrix2.txt"));
        file1.WriteLine("10 4 5");
        file1.WriteLine("8 9 1");
        file2.WriteLine("2 9");       
        file2.WriteLine("1 7");
        file2.WriteLine("4 19");
        file1.Close();
        file2.Close();
        var matrix1 = new DenseMatrix("matrix1.txt");
        var matrix2 = new DenseMatrix("matrix2.txt");
        Assert.AreEqual(matrix1.Matrix, new[,] { { 10, 4, 5 }, { 8, 9, 1 } });
        Assert.AreEqual(matrix2.Matrix, new[,] { { 2, 9 }, { 1, 7 }, { 4, 19 } });
        File.Delete("matrix1.txt");
        File.Delete("matrix2.txt");
    }
    
    [Test]
    public void SerialCalculationsAreCorrect()
    {
        var correctResult = new[,] { { 44, 213 }, { 29, 154 } };
        var matrix1 = new DenseMatrix(new[,] { { 10, 4, 5 }, { 8, 9, 1 } });
        var matrix2 = new DenseMatrix(new[,] { { 2, 9 }, { 1, 7 }, {4, 19} });
        var result = DenseMatrix.MultiplySerially(matrix1, matrix2);
        Assert.AreEqual(correctResult, result.Matrix);
    }
    
    [Test]
    public void SerialCalculationsAndConcurrentCalculationsAreEqual()
    {
        for (int i = 0; i < 10; i++)
        {
            DenseMatrix.MatrixGenerator("matrix1.txt", (9, 10));
            DenseMatrix.MatrixGenerator("matrix2.txt", (10, 8));
            var matrix1 = new DenseMatrix("matrix1.txt");
            var matrix2 = new DenseMatrix("matrix2.txt");
            var result1 = DenseMatrix.MultiplyConcurrently(matrix1, matrix2);
            var result2 = DenseMatrix.MultiplySerially(matrix1, matrix2);
            Assert.AreEqual(result1.Matrix, result2.Matrix);
        }
    }

    [Test]
    public void SerialAndConcurrentCalculationsCanMultiply1X1Matrices()
    {
        var matrix1 = new DenseMatrix(new[,] { { 9 } });
        var matrix2 = new DenseMatrix(new[,] { { 100 } });
        var result1 = DenseMatrix.MultiplyConcurrently(matrix1, matrix2);
        var result2 = DenseMatrix.MultiplySerially(matrix1, matrix2);
        Assert.AreEqual(result1.Matrix, new[,] { { 900 } });
        Assert.AreEqual(result1.Matrix, result2.Matrix);
    }

    [Test]
    public void SerialAndConcurrentCalculationsCanMultiplyVectors()
    {
        DenseMatrix.MatrixGenerator("matrix1.txt", (1, 900));
        DenseMatrix.MatrixGenerator("matrix2.txt", (900, 1));
        var matrix1 = new DenseMatrix("matrix1.txt");
        var matrix2 = new DenseMatrix("matrix2.txt");
        var result1 = DenseMatrix.MultiplyConcurrently(matrix1, matrix2);
        var result2 = DenseMatrix.MultiplySerially(matrix1, matrix2);
        Assert.AreEqual(result1.Matrix, result2.Matrix);
    }
}