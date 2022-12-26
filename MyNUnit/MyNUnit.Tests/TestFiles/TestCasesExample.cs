namespace MyNUnit.Tests.TestFiles;

using Attributes;
using Exceptions;

public class TestCasesExample
{
    [Test]
    public void Success()
    {
        var number = 0;

        for (var i = 0; i < 100; i++)
        {
            number += i;
        }
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

    [Test]
    public void UnexpectedException()
    {
        throw new TestException();
    }

    [Test]
    public int NonVoidReturnType()
    {
        return 50;
    }

    [Test]
    public void HasArguments(int number)
    { 
        for (var i = 0; i < 100; i++)
        {
            number += i;
        }
    }

    [Test]
    private void PrivateMethod()
    {
        var number = 0;

        for (var i = 0; i < 100; i++)
        {
            number += i;
        }
    }
}