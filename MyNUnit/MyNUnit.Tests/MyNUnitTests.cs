using MyNUnit.Internal;
using MyNUnit.Tests.TestFiles;

namespace MyNUnit.Tests;

public class MyNUnitTests
{
    [Test]
    public void TestAbstractClass()
    {
        var testType = new TestType(typeof(AbstractClassExample));
        testType.Run();
        Assert.That(testType.GeneralStatus, Is.EqualTo(Status.AbstractType));
    }

    [TestCase(typeof(BeforeExample))]
    [TestCase(typeof(BeforeClassExample))]
    [TestCase(typeof(AfterExample))]
    [TestCase(typeof(AfterClassExample))]
    public void TestAssistantMethods(Type classExample)
    {
        var testType = new TestType(classExample);
        testType.Run();
        var actualStatus = testType.TestUnitList.First().GeneralStatus; 
        Assert.That(actualStatus, Is.EqualTo(Status.Succeed));
    }
    
    [Test]
    public void TestMethods()
    {
        var testType = new TestType(typeof(TestCasesExample));
        testType.Run();

        foreach (var testUnit in testType.TestUnitList)
        {
            Assert.That(testUnit.TestStatus, Is.EqualTo(GetExpectedStatus(testUnit)));
        }
    }

    private Status GetExpectedStatus(TestUnit testUnit) =>
        testUnit switch
        {
            { Method.Name: "Success" } => Status.Succeed,
            { Method.Name: "Ignore" } => Status.Ignored,
            { Method.Name: "ExpectedException" } => Status.CaughtExpectedException,
            { Method.Name: "UnexpectedException" } => Status.Failed,
            { Method.Name: "NonVoidReturnType" } => Status.NonVoidMethod,
            { Method.Name: "HasArguments" } => Status.MethodHasArguments,
            { Method.Name: "PrivateMethod" } => Status.NonPublicMethod,
        };
}