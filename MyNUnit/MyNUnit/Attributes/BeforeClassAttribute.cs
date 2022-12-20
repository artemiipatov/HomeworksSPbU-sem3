namespace MyNUnit.Attributes;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class BeforeClassAttribute : MyNUnitAttribute
{
}