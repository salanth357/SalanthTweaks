using System.Linq;
using System.Xml;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game.WKS;
using FFXIVClientStructs.FFXIV.Client.System.Memory;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using InteropGenerator.Runtime;
using SalanthTweaks.Enums;
using SalanthTweaks.Interfaces;

namespace SalanthTweaks.Tweaks;

[RegisterSingleton<ITweak>(Duplicate = DuplicateStrategy.Append)]
public class WKSResearchMenu : ITweak
{
    public string DisplayName => "WKSResearchMenu";
    public TweakStatus Status { get; set; }

    private unsafe delegate nint AddonCreateDelegate(RaptureAtkModule* thisPtr, CStringPointer addonName, uint valueCount, AtkValue* values);
    private Hook<AddonCreateDelegate>? addonCreateHook;
    public void OnInitialize() { }

    public void Dispose() { }
    
    public unsafe void OnEnable()
    {
        var addonIndex =
            RaptureAtkModule.Instance()->AddonNames.FindIndex(x => x.EqualToString("SelectString"));
        
        addonCreateHook = Service.Get<IGameInteropProvider>().HookFromAddress<AddonCreateDelegate>(
            RaptureAtkModule.Instance()->AddonFactories[addonIndex].Create, AddonCreateDetour);
        addonCreateHook?.Enable();
    }

    public void OnDisable()
    {
        addonCreateHook?.Disable();
        addonCreateHook?.Dispose();
    } 
    
    private unsafe delegate bool IsNextAvailableDG(WKSResearchModule* thisPtr, byte jobIndex);

    [Signature("E8 ?? ?? ?? ?? 41 0F B6 57 ?? 84 C0")]
    private unsafe IsNextAvailableDG? isNextAvailable = null;

    private unsafe nint AddonCreateDetour(RaptureAtkModule* thisPtr, CStringPointer addonName, uint valueCount, AtkValue* valuesP)
    {
        if (isNextAvailable == null) goto noChange;
        if (valueCount <= 8 || !valuesP[8].String.ToString().Equals("Inquire about cosmic tools.")) 
            goto noChange;
        if (!Enumerable.Range(1, 11).Any(x => isNextAvailable.Invoke(WKSManager.Instance()->ResearchModule, (byte)x)))
            goto noChange;

        var itemCount = valueCount - 7;
        var newValueCount = 6 + (itemCount * 3);

        var newValuesP = stackalloc AtkValue[(int)newValueCount];
        var newValues = new Span<AtkValue>(newValuesP, (int)newValueCount);
        
        newValues[3].Copy(&valuesP[2]);

        var newOffset = 6;
        for (var i = 0; i < itemCount; i++)
        {
            newValues[newOffset++].SetInt(0);
            newValues[newOffset++].Copy(&valuesP[i + 7]);
            newValues[newOffset++].SetInt(0);
        }

        newValues[6].SetInt(61416);

        var sisIndex =
            RaptureAtkModule.Instance()->AddonNames.FindIndex(x => x.EqualToString("SelectIconString"));

        var ret = RaptureAtkModule.Instance()->AddonFactories[sisIndex].Create(
            thisPtr, RaptureAtkModule.Instance()->AddonNames[sisIndex].StringPtr, newValueCount, newValuesP);
        
        foreach (ref var v in  newValues)
            v.Dtor();

        AtkUnitBase b;
        return (nint)ret;
        
        
        noChange:
        return addonCreateHook!.Original.Invoke(thisPtr, addonName, valueCount, valuesP);
    }
}

