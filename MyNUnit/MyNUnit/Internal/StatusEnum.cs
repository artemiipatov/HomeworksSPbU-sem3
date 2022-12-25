namespace MyNUnit.Internal;

public enum Status
{
    Succeed,
    IsRunning,
    Failed,
    CaughtExpectedException,
    StaticMethod,
    NonStaticMethod,
    NonVoidMethod,
    NonPublicMethod,
    MethodHasArguments,
    AbstractType,
    Ignored,
}