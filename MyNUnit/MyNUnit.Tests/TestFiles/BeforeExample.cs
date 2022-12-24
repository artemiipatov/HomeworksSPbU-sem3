using MyNUnit.Attributes;

namespace MyNUnit.Tests.TestFiles;

public class BeforeExample
{
    public int Field = 100;

    [Before]
    public void Before()
    {
        Field -= 50;
    }

    [Attributes.Test]
    public void Test()
    {
    }
}