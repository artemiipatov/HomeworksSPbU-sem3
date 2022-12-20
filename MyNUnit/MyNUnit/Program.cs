using MyNUnit;

if (args.Length == 0)
{
    throw new InvalidDataException("No path given.");
}

var myNUnit = new MyNUnit.MyNUnit();
myNUnit.Run(args);

IPrinter printer = new Printer();
myNUnit.AcceptPrinter(printer);