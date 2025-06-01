using FFXIVClientStructs.FFXIV.Client.System.Input;
using SalanthTweaks.Enums;
using SalanthTweaks.Interfaces;

namespace SalanthTweaks.Tweaks;

[RegisterSingleton<ITweak>(Duplicate = DuplicateStrategy.Append)]
public class FailTweak : ITweak
{
    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    public string DisplayName => "Fail Tweak";
    public TweakStatus Status { get; set; } = TweakStatus.Uninitialized;

    public void OnInitialize()
    {
        throw new NotImplementedException();
    }

    public void OnEnable()
    {
    }

    public void OnDisable()
    {
    }
}
