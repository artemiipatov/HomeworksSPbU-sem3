namespace MyNUnit.Attributes;

using Optional;

[AttributeUsage(AttributeTargets.Method)]
public class TestAttribute : MyNUnitAttribute
{
    public TestAttribute()
    {
    }

    public TestAttribute(string ignore) => Ignore = ignore;

    public TestAttribute(Type expected) => Expected = expected.Some();

    public TestAttribute(Type expected, string ignore)
    {
        Expected = expected.Some();
        Ignore = ignore;
    }

    public string Ignore { get; } = string.Empty;

    public Option<Type> Expected { get; } = Option.None<Type>();
}