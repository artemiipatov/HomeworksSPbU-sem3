namespace MyNUnit;

public enum Status
{
    IsRunning,
    Succeed,
    TestSucceed,
    AfterSucceed,
    BeforeClassSucceed,
    AfterClassSucceed,
    Failed,
    BeforeFailed,
    AfterFailed,
    TestFailed,
    BeforeClassFailed,
    AfterClassFailed,
    CaughtExpectedException,
    StaticMethod,
    NonStaticMethod,
    NonVoidMethod,
    NonPublicMethod,
    MethodHasArguments,
    Ignored,
}