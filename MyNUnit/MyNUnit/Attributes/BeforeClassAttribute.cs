namespace MyNUnit.Attributes;

/// <summary>
/// Identifies a static method that is called once prior to executing all <see cref="MyNUnitAttribute"/> methods.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class BeforeClassAttribute : MyNUnitAttribute
{
}