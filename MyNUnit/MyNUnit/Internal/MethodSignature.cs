namespace MyNUnit.Internal;

using System.Reflection;

public class MethodSignature
{
    public MethodSignature(MethodInfo method)
    {
        this.Method = method;
    }

    public MethodInfo Method { get; }

    public bool IsPublic => this.Method.IsPublic;

    public bool IsStatic => this.Method.IsStatic;

    public bool HasArguments => this.Method.GetParameters().Length != 0;

    public bool IsVoid => this.Method.ReturnType == typeof(void);
}