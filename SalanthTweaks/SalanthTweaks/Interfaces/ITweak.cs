using SalanthTweaks.Enums;

namespace SalanthTweaks.Interfaces;

public interface ITweak : IDisposable
{
    public string InternalName => GetType().Name;
    string DisplayName { get; }
    TweakStatus Status { get; set; }
    void OnInitialize();
    void OnEnable();
    void OnDisable();
}
