using System.Reflection;

namespace MyNUnit;

public class MethodSignature
{
    public MethodSignature(MethodInfo method)
    {
        this.Method = method;
    }

    public MethodInfo Method { get; }

    public bool IsPublic => Method.IsPublic;

    public bool IsStatic => Method.IsStatic;

    public bool HasArguments => Method.GetParameters().Length != 0;

    public bool IsVoid => Method.ReturnType == typeof(void);
}