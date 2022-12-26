namespace MyNUnit.Internal;

public enum TestTypeStatus
{
    /// <summary>
    /// There are no failed tests.
    /// </summary>
    Succeed,

    /// <summary>
    /// Tests are still running.
    /// </summary>
    IsRunning,

    /// <summary>
    /// At least one of tests failed.
    /// </summary>
    TestsFailed,

    /// <summary>
    /// One of BeforeClass methods failed.
    /// </summary>
    BeforeClassFailed,

    /// <summary>
    /// One of AfterClass methods failed.
    /// </summary>
    AfterClassFailed,

    /// <summary>
    /// One of BeforeClass/AfterClass methods is not static.
    /// </summary>
    NonStaticMethod,

    /// <summary>
    /// One of BeforeClass/AfterClass methods has non void return type.
    /// </summary>
    NonVoidMethod,

    /// <summary>
    /// One of BeforeClass/AfterClass methods is not public.
    /// </summary>
    NonPublicMethod,

    /// <summary>
    /// One of BeforeClass/AfterClass methods has arguments.
    /// </summary>
    MethodHasArguments,

    /// <summary>
    /// Test type is abstract.
    /// </summary>
    AbstractType,
}