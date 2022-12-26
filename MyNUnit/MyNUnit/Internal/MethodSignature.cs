namespace MyNUnit.Internal;

using System.Reflection;

/// <summary>
/// Class that represents method signature.
/// </summary>
public class MethodSignature
{

    /// <summary>
    /// Initializes a new instance of the <see cref="MethodSignature"/> class.
    /// </summary>
    /// <param name="method">Method.</param>
    public MethodSignature(MethodInfo method)
    {
        this.Method = method;
    }

    /// <summary>
    /// Gets the MethodInfo of method.
    /// </summary>
    public MethodInfo Method { get; }

    /// <summary>
    /// Gets a value indicating whether the method is public.
    /// </summary>
    public bool IsPublic => this.Method.IsPublic;

    /// <summary>
    /// Gets a value indicating whether the method is static.
    /// </summary>
    public bool IsStatic => this.Method.IsStatic;

    /// <summary>
    /// Gets a value indicating whether the method has arguments.
    /// </summary>
    public bool HasArguments => this.Method.GetParameters().Length != 0;

    /// <summary>
    /// Gets a value indicating the return type of the method is void.
    /// </summary>
    public bool IsVoid => this.Method.ReturnType == typeof(void);
}