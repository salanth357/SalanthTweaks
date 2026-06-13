using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace SalanthTweaks.Structs;

[StructLayout(LayoutKind.Explicit, Size = 0x398)]
public struct AddonMKDRelicGrowth
{
    
    [FieldOffset(0x0)]
    public AtkUnitBase Base;

    [FieldOffset(0x238)]
    internal FixedSizeArray4<Aetherwell> _wells;

    [System.Diagnostics.CodeAnalysis.UnscopedRefAttribute]
    public Span<Aetherwell> Wells => _wells;

    [StructLayout(LayoutKind.Sequential, Size = 0x58)]
    public unsafe struct Aetherwell
    {
        public AtkComponentButton* Button;
        public AtkComponentTextNineGrid* Tooltip;
        public AtkComponentBase* Icon;
        public AtkResNode* BaseRes;
        public AtkResNode* Symbol;
        public AtkResNode* FillRes;
        public AtkResNode* FilledEffectRes;
        public AtkResNode* GlowingBackgroundRes;
        public AtkTextNode* CurrentAmount;
        public AtkTextNode* MaxAmount;
        public int Unk50;
        // 4 bytes padding
    }
}
