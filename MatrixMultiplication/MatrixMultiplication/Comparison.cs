namespace MatrixMultiplication;

/// <summary>
/// Tools for matrix multiplication comparison.
/// </summary>
public static class Comparison
{
    /// <summary>
    /// Calculates standard deviation using time of matrix multiplications and number of multiplications.
    /// </summary>
    /// <param name="calculationTime">Array, which elements are time of matrix multiplications.</param>
    /// <returns>Returns double value -- rounded to one decimal place standard deviation of the given data.</returns>
    public static double CalculateDeviation(long[] calculationTime)
    {
        var expectedValue = calculationTime.Average();
        double variance = 0;

        for (var i = 0; i < calculationTime.Length; i++)
        {
            variance += Math.Pow(calculationTime[i] - expectedValue, 2) / calculationTime.Length;
        }

        return Math.Round(Math.Sqrt(variance), 1);
    }
}