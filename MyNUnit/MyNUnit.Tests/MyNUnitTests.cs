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
        Assert.That(testType.Status, Is.EqualTo(TestTypeStatus.AbstractType));
    }

    [TestCase(typeof(BeforeExample))]
    [TestCase(typeof(BeforeClassExample))]
    [TestCase(typeof(AfterExample))]
    [TestCase(typeof(AfterClassExample))]
    public void TestAssistantMethods(Type classExample)
    {
        var testType = new TestType(classExample);
        testType.Run();
        var actualStatus = testType.TestUnitList.First().Status; 
        Assert.That(actualStatus, Is.EqualTo(TestUnitStatus.Succeed));
    }
    
    [Test]
    public void TestMethods()
    {
        var testType = new TestType(typeof(TestCasesExample));
        testType.Run();

        foreach (var testUnit in testType.TestUnitList)
        {
            Assert.That(testUnit.Status, Is.EqualTo(GetExpectedStatus(testUnit)));
        }
    }
  
    private TestUnitStatus GetExpectedStatus(TestUnit testUnit) =>
        testUnit switch
        {
            { Method.Name: "Success" } => TestUnitStatus.Succeed,
            { Method.Name: "Ignore" } => TestUnitStatus.Ignored,
            { Method.Name: "ExpectedException" } => TestUnitStatus.CaughtExpectedException,
            { Method.Name: "UnexpectedException" } => TestUnitStatus.TestFailed,
            { Method.Name: "NonVoidReturnType" } => TestUnitStatus.NonVoidMethod,
            { Method.Name: "HasArguments" } => TestUnitStatus.MethodHasArguments,
            { Method.Name: "PrivateMethod" } => TestUnitStatus.NonPublicMethod,
        };
     
}