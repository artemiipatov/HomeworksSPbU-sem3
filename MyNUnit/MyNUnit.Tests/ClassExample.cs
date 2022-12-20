namespace MyNUnit.Tests;

using Attributes;

public class ClassExample
{
    [BeforeClass]
    public static void BeforeClass()
    {
        
    }

    [Test]
    public void Fail()
    {
        
    }

    [Test]
    public void Success()
    {
        
    }

    [Test("Ignore")]
    public void Ignore()
    {
        throw new TestException();
    }

    [Test(typeof(TestException))]
    public void ExpectedException()
    {
        throw new TestException();
    }

    public void UnexpectedException()
    {
        throw new TestException();
    }
    
    [AfterClass]
    public void AfterClass()
    {

    }
}