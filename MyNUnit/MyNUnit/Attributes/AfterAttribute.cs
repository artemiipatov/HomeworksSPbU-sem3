namespace MyNUnit.Attributes;

/// <summary>
/// Identifies a method that is called every time after executing <see cref="TestAttribute"/> method.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class AfterAttribute : MyNUnitAttribute
{
}