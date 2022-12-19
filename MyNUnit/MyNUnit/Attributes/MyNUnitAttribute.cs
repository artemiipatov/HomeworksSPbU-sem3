namespace MyNUnit.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public abstract class MyNUnitAttribute : Attribute
{
    public MyNUnitAttribute()
    {
    }
}