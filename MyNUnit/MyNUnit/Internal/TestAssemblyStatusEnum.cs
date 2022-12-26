namespace MyNUnit.Internal;

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