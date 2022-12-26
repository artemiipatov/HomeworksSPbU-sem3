namespace MyNUnit.Internal;

/// <summary>
/// Represents current status of the <see cref="TestAssembly"/>.
/// </summary>
public enum TestAssemblyStatus
{
    /// <summary>
    /// There are no failed tests in the assembly.
    /// </summary>
    Succeed,

    /// <summary>
    /// At least one of tests in the assembly failed.
    /// </summary>
    Failed,
}