namespace MyNUnit.Attributes;

using Optional;

/// <summary>
/// Marks the method as a test.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class TestAttribute : MyNUnitAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TestAttribute"/> class.
    /// </summary>
    public TestAttribute()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TestAttribute"/> class.
    /// </summary>
    /// <param name="ignore">Reason of ignoring the test method.</param>
    public TestAttribute(string ignore) => Ignore = ignore.Some();

    /// <summary>
    /// Initializes a new instance of the <see cref="TestAttribute"/> class.
    /// </summary>
    /// <param name="expected">Type of expected exception.</param>
    public TestAttribute(Type expected) => Expected = expected.Some();

    /// <summary>
    /// Initializes a new instance of the <see cref="TestAttribute"/> class.
    /// </summary>
    /// <param name="expected">Type of expected exception.</param>
    /// <param name="ignore">Reason of ignoring test method.</param>
    public TestAttribute(Type expected, string ignore)
    {
        Expected = expected.Some();
        Ignore = ignore.Some();
    }

    /// <summary>
    /// Gets reason of ignoring test method.
    /// </summary>
    public Option<string> Ignore { get; } = Option.None<string>();

    /// <summary>
    /// Gets type of expected exception.
    /// </summary>
    public Option<Type> Expected { get; } = Option.None<Type>();
}