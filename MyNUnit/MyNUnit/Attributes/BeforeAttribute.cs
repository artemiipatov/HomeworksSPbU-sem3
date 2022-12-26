namespace MyNUnit.Attributes;

/// <summary>
/// Identifies a method that is called every time prior to executing <see cref="TestAttribute"/> methods.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class BeforeAttribute : MyNUnitAttribute
{
}