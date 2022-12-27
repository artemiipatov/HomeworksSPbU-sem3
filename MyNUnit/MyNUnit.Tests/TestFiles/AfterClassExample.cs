using MyNUnit.Attributes;

namespace MyNUnit.Tests.TestFiles;

public class AfterClassExample
{
    public static int StaticVariable = 100;

    [Attributes.Test]
    public void Test()
    {
    }
    
    [AfterClass]
    public static void AfterClass()
    {
        StaticVariable -= 50;
    }
}