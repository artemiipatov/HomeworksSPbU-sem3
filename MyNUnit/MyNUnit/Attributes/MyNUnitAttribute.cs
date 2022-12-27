namespace MyNUnit.Attributes;

/// <summary>
/// Base attribute for all MyNUnit attributes.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public abstract class MyNUnitAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MyNUnitAttribute"/> class.
    /// </summary>
    public MyNUnitAttribute()
    {
    }
}