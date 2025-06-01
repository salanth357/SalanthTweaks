namespace SalanthTweaks.Config;

[Serializable]
public abstract class TweakConfig
{
    public int Version { get; set; } = 0;

    public abstract bool Update();
}
