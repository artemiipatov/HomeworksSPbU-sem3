using MyNUnit;
using MyNUnit.Printer;

if (args.Length == 0)
{
    throw new InvalidDataException("No path given.");
}

var myNUnit = new MyNUnit.Internal.MyNUnit();
myNUnit.Run(args);

IPrinter printer = new Printer();
myNUnit.AcceptPrinter(printer);