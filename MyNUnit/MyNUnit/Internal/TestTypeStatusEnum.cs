namespace MyNUnit.Internal;

public enum TestTypeStatus
{
    /// <summary>
    /// Tests passed.
    /// </summary>
    Succeed,

    /// <summary>
    /// Tests is still running.
    /// </summary>
    IsRunning,

    /// <summary>
    /// Tests failed.
    /// </summary>
    TestsFailed,

    BeforeClassFailed,

    AfterClassFailed,

    /// <summary>
    /// TestAn expected exception was caught wile running test
    /// </summary>
    NonStaticMethod,
    NonVoidMethod,
    NonPublicMethod,
    MethodHasArguments,
    AbstractType,
}