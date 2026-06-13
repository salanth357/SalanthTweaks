using JetBrains.Annotations;

namespace SalanthTweaks.Attributes;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
[MeansImplicitUse]
public class TweakConfigAttribute : Attribute;
