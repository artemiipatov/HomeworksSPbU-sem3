namespace MyNUnit.Attributes;

using Optional;

[AttributeUsage(AttributeTargets.Method)]
public class TestAttribute : MyNUnitAttribute
{
    public TestAttribute()
    {
    }

    public TestAttribute(string ignore) => Ignore = ignore.Some();

    public TestAttribute(Type expected) => Expected = expected.Some();

    public TestAttribute(Type expected, string ignore)
    {
        Expected = expected.Some();
        Ignore = ignore.Some();
    }

    public Option<string> Ignore { get; } = Option.None<string>();

    public Option<Type> Expected { get; } = Option.None<Type>();
}