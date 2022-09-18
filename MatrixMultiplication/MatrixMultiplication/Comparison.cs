namespace MatrixMultiplication;

/// <summary>
/// Tools for matrix multiplication comparison.
/// </summary>
public static class Comparison
{
    /// <summary>
    /// Calculates standard deviation using time of matrix multiplications and number of multiplications.
    /// </summary>
    /// <param name="N"></param>
    /// <param name="calculationTime"></param>
    /// <returns></returns>
    public static double CalculateDeviation(int N, long[] calculationTime)
    {
        var expectedValue = CalculateExpectedValue(N, calculationTime);
        double variance = 0;
        for (var i = 0; i < N; i++)
        {
            variance += Math.Pow(calculationTime[i] - expectedValue, 2) / N;
        }

        return Math.Sqrt(variance);
    }

    /// <summary>
    /// Calculates expected value (which is arithmetic average in the context of matrix multiplication time).
    /// </summary>
    /// <param name="N">Number of multiplications.</param>
    /// <param name="calculationTime">Array, which elements are time of matrix multiplications.</param>
    /// <returns></returns>
    public static long CalculateExpectedValue(int N, long[] calculationTime) => calculationTime.Sum() / N;
}