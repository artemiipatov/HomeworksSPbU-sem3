if (args.Length == 0)
{
    throw new InvalidDataException("No path given.");
}

var myNUnit = new MyNUnit.MyNUnit();
myNUnit.RunTestsFromAllAssemblies(args);
Thread.Sleep(500);
myNUnit.Print();