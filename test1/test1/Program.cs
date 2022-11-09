using System.Diagnostics;
using test1;

if (args.Length != 1)
{
    return;
}

const int n = 10;

var calculationTime = new long[n];
for (var i = 0; i < n; i++)
{
    var stopwatch = new Stopwatch();
    stopwatch.Start();
    CheckSumCalculator.CalculateCheckSumSerially(args[0]);
    stopwatch.Stop();
    calculationTime[i] = stopwatch.ElapsedMilliseconds;
}

var expectedValue1 = calculationTime.Average();
var deviation1 = Comparison.CalculateDeviation(calculationTime);
Console.WriteLine(expectedValue1);
Console.WriteLine(deviation1);

for (var i = 0; i < n; i++)
{
    var stopwatch = new Stopwatch();
    stopwatch.Start();
    CheckSumCalculator.CalculateCheckSumConcurrently(args[0]);
    stopwatch.Stop();
    calculationTime[i] = stopwatch.ElapsedMilliseconds;
}

var expectedValue2 = calculationTime.Average();
var deviation2 = Comparison.CalculateDeviation(calculationTime);
Console.WriteLine(expectedValue2);
Console.WriteLine(deviation2);
