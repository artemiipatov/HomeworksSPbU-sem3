namespace MyNUnit;

public interface IPrinter
{
    void PrintMyNUnitInfo(MyNUnit myNUnit);

    void PrintAssemblyInfo(TestAssembly testAssembly);

    void PrintTestClassInfo(TestClass testClass);

    void PrintTestUnitInfo(TestUnit testUnit);
}