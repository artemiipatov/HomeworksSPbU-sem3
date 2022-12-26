namespace MyNUnit.Attributes;

/// <summary>
/// Base attribute for all MyNUnit attributes.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public abstract class MyNUnitAttribute : Attribute
{
    public MyNUnitAttribute()
    {
    }
}