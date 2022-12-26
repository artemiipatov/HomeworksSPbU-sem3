namespace MyNUnit.Internal;

public enum TestUnitStatus
{
    IsRunning,

    Succeed,
    
    TestFailed,
    
    BeforeFailed,
    
    AfterFailed,
    
    NonVoidMethod,
    
    NonPublicMethod,
    
    MethodHasArguments,
    
    StaticMethod,

    CaughtExpectedException,

    Ignored,
}