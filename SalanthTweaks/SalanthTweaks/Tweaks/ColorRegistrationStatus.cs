using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lumina.Text;
using SalanthTweaks.Attributes;
using SalanthTweaks.Enums;
using SalanthTweaks.Interfaces;

namespace SalanthTweaks.Tweaks;

[RegisterSingleton<ITweak>(Duplicate = DuplicateStrategy.Append)]
#pragma warning disable SeStringEvaluator
public class ColorRegistrationStatus(ISeStringEvaluator stringEvaluator) : ITweak
#pragma warning restore SeStringEvaluator
{
    public string DisplayName => "Color Registration Status";
    public string Description => "Applies a color to the obtained/registered labels in tooltips.";
    public TweakStatus Status { get; set; }

    private unsafe Utf8String* obtainedString;
    private unsafe Utf8String* macroObtainedString;
    private unsafe Utf8String* unobtainedString;
    private unsafe Utf8String* macroUnobtainedString;

    public unsafe void OnInitialize()
    {
        var sb = new SeStringBuilder();

        var s = stringEvaluator.EvaluateFromAddon(2498, [1]);
        obtainedString = Utf8String.FromSequence(s.AsSpan());
        sb.PushColorType(45).PushEdgeColorType(46).Append(s).PopColor().PopEdgeColor();
        macroObtainedString = Utf8String.FromSequence(sb.GetViewAsSpan());
        sb.Clear();

        s = stringEvaluator.EvaluateFromAddon(2498, [0]);
        unobtainedString = Utf8String.FromSequence(s.AsSpan());
        sb.PushColorType(17).PushEdgeColorType(18).Append(s).PopColor().PopEdgeColor();
        macroUnobtainedString = Utf8String.FromSequence(sb.GetViewAsSpan());
    }

    public unsafe void Dispose()
    {
        obtainedString->Dtor(true);
        macroObtainedString->Dtor(true);
        unobtainedString->Dtor(true);
        macroUnobtainedString->Dtor(true);
    }

    public void OnEnable() { }

    public void OnDisable() { }

    private unsafe delegate Utf8String* ItemFormatDelegate(AgentItemDetail* a1, Utf8String* a2, InventoryItem* a3, nint a4, uint a5, nint a6, uint a7, nint a8, byte a9);

    [AutoHook]
    [Signature("E8 ?? ?? ?? ?? 48 8B C8 E8 ?? ?? ?? ?? 49 8D 4F 08", DetourName = nameof(Detour))]
    private Hook<ItemFormatDelegate> hookItemFormat = null!;

    public unsafe Utf8String* Detour(AgentItemDetail* hookThis, Utf8String* outString, InventoryItem* invItem, nint itemSheetRow, uint itemId, nint a6, uint a7, nint a8, byte a9)
    {
        var newStr = hookItemFormat.Original(hookThis, outString, invItem, itemSheetRow, itemId, a6, a7, a8, a9);
        if (newStr != null)
        {
            Replace(obtainedString, macroObtainedString);
            Replace(unobtainedString, macroUnobtainedString);
        }
        return newStr;

        void Replace(Utf8String* needle, Utf8String* replacement)
        {
            var startIdx = newStr->IndexOf(needle);
            if (startIdx == -1) return;

            var tmpStr = Utf8String.CreateEmpty();
            newStr->CopySubStrTo(tmpStr, 0, startIdx);
            tmpStr->Append(replacement);
            if (startIdx + needle->Length < newStr->Length)
                tmpStr->ConcatCStr(newStr->Slice(startIdx + needle->Length));
            newStr->SetString(tmpStr->StringPtr);
        }
    }
}

