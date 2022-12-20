namespace MyNUnit;

public class Printer : IPrinter
{
    public void PrintMyNUnitInfo(MyNUnit myNUnit)
    {
        Console.WriteLine("MyNUnit");
        foreach (var assemblyTests in myNUnit.TestAssemblyList)
        {
            assemblyTests.AcceptPrinter(this);
        }
    }

    public void PrintAssemblyInfo(TestAssembly testAssembly)
    {
        Console.WriteLine(testAssembly.Assembly.GetName());
        foreach (var testClass in testAssembly.TestClassList)
        {
            testClass.AcceptPrinter(this);
        }
    }

    public void PrintTestClassInfo(TestClass testClass)
    {
        if (testClass.BeforeClassExceptionInfo.Length != 0)
        {
            Console.WriteLine(testClass.BeforeClassExceptionInfo);
        }

        Console.WriteLine(testClass.ClassType);

        foreach (var testUnit in testClass.TestUnitList)
        {
            testUnit.AcceptPrinter(this);
        }

        if (testClass.AfterClassExceptionInfo.Length != 0)
        {
            Console.WriteLine(testClass.AfterClassExceptionInfo);
        }
    }

    public void PrintTestUnitInfo(TestUnit testUnit)
    {
        Console.WriteLine(testUnit.Method.Name);

        var message = testUnit.CurrentStatus switch
        {
            Status.Succeed => $"Succeed. Time: {testUnit.Time} ms.",
            Status.Failed => $"Failed. Time: {testUnit.Time} ms."
                                      + Environment.NewLine
                                      + testUnit.ExceptionInfo,
            Status.Ignored => $"Ignored. Reason: {testUnit.Ignore}",
            _ => throw new ArgumentOutOfRangeException()
        };

        Console.WriteLine(message);
    }
}