using JetBrains.Annotations;

namespace SalanthTweaks.Attributes;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
[MeansImplicitUse]
public class AutoHookAttribute(bool enable = true, bool disable = true) : Attribute
{
    public bool Enable { get; } = enable;
    public bool Disable { get; } = disable;
}
