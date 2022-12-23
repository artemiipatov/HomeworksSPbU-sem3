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
        foreach (var testClass in testAssembly.TestTypeList)
        {
            testClass.AcceptPrinter(this);
        }
    }

    public void PrintTestTypeInfo(TestType testType)
    {
        var testTypeHeader = $"{testType.TypeOf} {testType.GeneralStatus} {testType.TestMethodsNumber}";

        var beforeClassMessage = testType.BeforeClassStatus switch
        {
            Status.Failed => $"Exception occured in one of BeforeClass methods."
                             + Environment.NewLine
                             + testType.ExceptionInfo,
            Status.NonPublicMethod => $"BeforeClass method should be public.",
            Status.NonStaticMethod => $"BeforeClass method should be static.",
            Status.NonVoidMethod => $"BeforeClass method should have void return type.",
            Status.MethodHasArguments => $"BeforeClass method should not have any arguments.",
            _ => string.Empty,
        };

        Console.WriteLine(testTypeHeader);
        Console.WriteLine(beforeClassMessage);

        foreach (var testUnit in testType.TestUnitList)
        {
            testUnit.AcceptPrinter(this);
        }

        var afterClassMessage = testType.AfterClassStatus switch
        {
            Status.Failed => $"Exception occured in one of BeforeClass methods."
                             + Environment.NewLine
                             + testType.ExceptionInfo,
            Status.NonPublicMethod => $"AfterClass method should be public.",
            Status.NonStaticMethod => $"AfterClass method should be static.",
            Status.NonVoidMethod => $"AfterClass method should have void return type.",
            Status.MethodHasArguments => $"AfterClass method should not have any arguments.",
            _ => string.Empty,
        };

        Console.WriteLine(afterClassMessage);
    }

    public void PrintTestUnitInfo(TestUnit testUnit)
    {
        var testUnitHeader = $"{testUnit.Method.Name}. Status: {testUnit.GeneralStatus}. Time: {testUnit.Time} ms.";

        var beforeMessage = testUnit.TestStatus switch
        {
            Status.Failed => "Exception occured in one of Before methods."
                             + Environment.NewLine
                             + testUnit.ExceptionInfo,
            Status.NonPublicMethod => $"Before method should be public.",
            Status.StaticMethod => $"Before method should be static.",
            Status.NonVoidMethod => $"Before method should have void return type.",
            Status.MethodHasArguments => $"Before method should not have any arguments.",
            _ => string.Empty,
        };

        var testMessage = testUnit.TestStatus switch
        {
            Status.Failed => "Exception occured in Test method.",
            Status.NonPublicMethod => $"Test method should be public.",
            Status.StaticMethod => $"Test method should be static.",
            Status.NonVoidMethod => $"Test method should have void return type.",
            Status.MethodHasArguments => $"Test method should not have any arguments.",
            _ => string.Empty,
        };

        var afterMessage = testUnit.AfterStatus switch
        {
            Status.Failed => $"Exception occured in one of After methods.",
            Status.NonPublicMethod => $"After method should be public.",
            Status.StaticMethod => $"After method should be static.",
            Status.NonVoidMethod => $"After method should have void return type.",
            Status.MethodHasArguments => $"After method should not have any arguments.",
            _ => string.Empty,
        };

        Console.WriteLine(testUnitHeader);
        Console.WriteLine(beforeMessage);
        Console.WriteLine(testMessage);
        Console.WriteLine(afterMessage);

        if (testUnit.TestStatus == Status.TestFailed
            || testUnit.AfterStatus == Status.TestFailed)
        {
            Console.WriteLine(testUnit.ExceptionInfo);
        }
    }
}