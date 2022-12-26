namespace MyNUnit.Printer;

using Internal;

public class Printer : IPrinter
{
    public void PrintMyNUnitInfo(MyNUnit myNUnit)
    {
        Console.WriteLine();

        foreach (var assemblyTests in myNUnit.TestAssemblyList)
        {
            assemblyTests.AcceptPrinter(this);
        }
    }

    public void PrintAssemblyInfo(TestAssembly testAssembly)
    {
        var testAssemblyHeader = $"Assembly name: {testAssembly.Assembly.GetName().Name}."
                                 + $"\nStatus: {testAssembly.Status}."
                                 + $"\nFailed: {testAssembly.FailedTestsCount}."
                                 + $" Succeeded: {testAssembly.SucceededTestsCount}."
                                 + $" Skipped: {testAssembly.SkippedTestsCount}.";

        Console.WriteLine(testAssemblyHeader);
        Console.WriteLine();

        foreach (var testClass in testAssembly.TestTypeList)
        {
            testClass.AcceptPrinter(this);
        }
    }

    public void PrintTestTypeInfo(TestType testType)
    {
        foreach (var testUnit in testType.TestUnitList)
        {
            testUnit.AcceptPrinter(this);
        }
    }

    public void PrintTestUnitInfo(TestUnit testUnit)
    {
        var testUnitHeader = $"Method name: {testUnit.Method.Name}."
                             + $"\nStatus: {testUnit.Status}."
                             + $"\nTime: {testUnit.Time} ms.";

        var testMessage = testUnit.Status switch
        {
            TestUnitStatus.CaughtExpectedException => $"An expected {testUnit.ExpectedExceptionName} was caught",
            TestUnitStatus.BeforeFailed => "An unexpected exception occured during Before method execution.",
            TestUnitStatus.TestFailed => "An unexpected exception occured during Test method execution."
                                         + $"\n{testUnit.ExceptionInfo}",
            TestUnitStatus.AfterFailed => "An unexpected exception occured during After method execution.",
            TestUnitStatus.NonPublicMethod => "Test or Before/After methods should be public.",
            TestUnitStatus.StaticMethod => "Test or Before/After methods should not be static.",
            TestUnitStatus.NonVoidMethod => "Test or Before/After methods should have void return type.",
            TestUnitStatus.MethodHasArguments => "Test or Before/After methods should not have arguments.",
            TestUnitStatus.Ignored => $"Test method was ignored. Reason: {testUnit.Ignore}",
            _ => string.Empty,
        };

        Console.WriteLine(testUnitHeader);
        Console.WriteLine(testMessage);
        Console.WriteLine();
    }
}