using MyNUnit.Attributes;

namespace MyNUnit.Tests.TestFiles;

public class BeforeClassExample
{
    public static int StaticVariable = 0; 
    
    [BeforeClass]
    public static void BeforeClass()
    {
        StaticVariable = 50;
    }

    [Attributes.Test]
    public void Test()
    {
    }
}