namespace MyNUnit.Printer;

using Internal;

/// <summary>
/// Represents set of methods for printing tests information.
/// </summary>
public interface IPrinter
{
    /// <summary>
    /// Prints information of current <see cref="MyNUnit"/> instance.
    /// </summary>
    /// <param name="myNUnit">Instance of <see cref="MyNUnit"/>.</param>
    void PrintMyNUnitInfo(MyNUnit myNUnit);

    /// <summary>
    /// Prints information of current <see cref="TestAssembly"/> instance.
    /// </summary>
    /// <param name="testAssembly">Instance of <see cref="TestAssembly"/>.</param>
    void PrintAssemblyInfo(TestAssembly testAssembly);

    /// <summary>
    /// Prints information of current <see cref="TestUnit"/> instance.
    /// </summary>
    /// <param name="testType">Instance of <see cref="TestUnit"/>.</param>
    void PrintTestTypeInfo(TestType testType);

    /// <summary>
    /// Prints information of current <see cref="TestUnit"/> instance.
    /// </summary>
    /// <param name="testUnit">Instance of <see cref="TestUnit"/>.</param>
    void PrintTestUnitInfo(TestUnit testUnit);
}