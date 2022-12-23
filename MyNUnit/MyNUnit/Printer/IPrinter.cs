namespace MyNUnit;

public interface IPrinter
{
    void PrintMyNUnitInfo(MyNUnit myNUnit);

    void PrintAssemblyInfo(TestAssembly testAssembly);

    void PrintTestTypeInfo(TestType testType);

    void PrintTestUnitInfo(TestUnit testUnit);
}