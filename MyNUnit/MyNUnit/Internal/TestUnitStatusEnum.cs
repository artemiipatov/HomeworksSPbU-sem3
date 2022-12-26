namespace MyNUnit.Internal;

/// <summary>
/// Represents current status of the <see cref="TestUnit"/>.
/// </summary>
public enum TestUnitStatus
{
    /// <summary>
    /// Test is still running.
    /// </summary>
    IsRunning,

    /// <summary>
    /// Test passed.
    /// </summary>
    Succeed,

    /// <summary>
    /// Expected exception was caught during test execution. Test passed.
    /// </summary>
    CaughtExpectedException,

    /// <summary>
    /// Test failed.
    /// </summary>
    TestFailed,

    /// <summary>
    /// One of before methods failed.
    /// </summary>
    BeforeFailed,

    /// <summary>
    /// One of after methods failed.
    /// </summary>
    AfterFailed,

    /// <summary>
    /// Test or one of Before/After methods has non void return type.
    /// </summary>
    NonVoidMethod,

    /// <summary>
    /// Test or one of Before/After methods is not public.
    /// </summary>
    NonPublicMethod,

    /// <summary>
    /// Test or one of Before/After methods has arguments.
    /// </summary>
    MethodHasArguments,

    /// <summary>
    /// Test or one of Before/After methods is static.
    /// </summary>
    StaticMethod,

    /// <summary>
    /// Test was ignored.
    /// </summary>
    Ignored,
}