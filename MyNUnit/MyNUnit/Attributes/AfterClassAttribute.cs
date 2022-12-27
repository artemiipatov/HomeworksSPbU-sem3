namespace MyNUnit.Attributes;

/// <summary>
/// Identifies a static method that is called once after executing all <see cref="TestAttribute"/> methods.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class AfterClassAttribute : MyNUnitAttribute
{
}