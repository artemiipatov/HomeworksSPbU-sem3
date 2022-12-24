using MyNUnit.Attributes;

namespace MyNUnit.Tests.TestFiles;

public class AfterExample
{
    public int Field = 100; 
    
    [Attributes.Test]
    public void Test()
    {
    }

    [After]
    public void After()
    {
        Field -= 50;
    }
}