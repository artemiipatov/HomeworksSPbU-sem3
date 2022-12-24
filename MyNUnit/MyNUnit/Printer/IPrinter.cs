using MyNUnit.Internal;

namespace MyNUnit.Printer;

public interface IPrinter
{
    void PrintMyNUnitInfo(Internal.MyNUnit myNUnit);

    void PrintAssemblyInfo(TestAssembly testAssembly);

    void PrintTestTypeInfo(TestType testType);

    void PrintTestUnitInfo(TestUnit testUnit);
}