using test1;

if (args.Length != 1)
{
    return;
}

var hash = CheckSumCalculator.CalculateCheckSumConcurrently(args[0]);
Console.WriteLine(BitConverter.ToString(hash));