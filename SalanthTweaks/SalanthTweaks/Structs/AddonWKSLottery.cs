using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Component.GUI;
using InteropGenerator.Runtime;

namespace SalanthTweaks.Structs;

[StructLayout(LayoutKind.Explicit, Size = 0x240)]
public unsafe struct AddonWKSLottery
{
    [FieldOffset(0)]
    AtkUnitBase Base;

    [FieldOffset(0x238)]
    public RendererHolder* SubObject;

    public Renderer* GetRenderer()
    {
        if (SubObject == null) return null;

        return &SubObject->Object;
    }


    [StructLayout(LayoutKind.Explicit, Size = 0x18B8)]
    public struct RendererHolder
    {
        [FieldOffset(0x10)]
        public Renderer Object;
    }

    [StructLayout(LayoutKind.Explicit, Size = 0x18A8)]
    public struct Renderer
    {
        [FieldOffset(0x248)]
        internal FixedSizeArray20<WKSLotteryWheelChunk> _leftWheelChunks;

        [System.Diagnostics.CodeAnalysis.UnscopedRefAttribute]
        public Span<WKSLotteryWheelChunk> LeftWheelChunks => _leftWheelChunks;

        [FieldOffset(0x568)]
        internal FixedSizeArray20<WKSLotteryWheelChunk> _rightWheelChunks;

        [System.Diagnostics.CodeAnalysis.UnscopedRefAttribute]
        public Span<WKSLotteryWheelChunk> RightWheelChunks => _rightWheelChunks;

        [FieldOffset(0x8F8)]
        internal FixedSizeArray7<WKSLotteryItem> _leftWheelItems;

        [System.Diagnostics.CodeAnalysis.UnscopedRefAttribute]
        public Span<WKSLotteryItem> LeftWheelItems => _leftWheelItems;

        [FieldOffset(0xB28)]
        internal FixedSizeArray7<WKSLotteryItem> _rightWheelItems;

        [System.Diagnostics.CodeAnalysis.UnscopedRefAttribute]
        public Span<WKSLotteryItem> RightWheelItems => _rightWheelItems;

        [FieldOffset(0xD58)]
        internal FixedSizeArray7<WKSLotteryItem> _thirdWheelItems;

        [System.Diagnostics.CodeAnalysis.UnscopedRefAttribute]
        public Span<WKSLotteryItem> ThirdWheelItems => _thirdWheelItems;


        [FieldOffset(0x1560)]
        internal FixedSizeArray20<WKSLotteryWheelChunk> _thirdWheelChunks;

        [System.Diagnostics.CodeAnalysis.UnscopedRefAttribute]
        public Span<WKSLotteryWheelChunk> ThirdWheelChunks => _thirdWheelChunks;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 8, Size = 0x28)]
    public struct WKSLotteryWheelChunk
    {
        public AtkComponentBase* WheelSegmentComponent;
        public AtkComponentBase* WheelIconComponent;
        public AtkResNode* WheelIconNode;
        public AtkResNode* WheelSegmentNode;
        public uint ItemIndex;
    }

    [StructLayout(LayoutKind.Explicit, Pack = 8, Size = 0x50)]
    public struct WKSLotteryItem
    {
        [FieldOffset(0x00)]
        public AtkComponentBase* RootComponent;

        [FieldOffset(0x08)]
        public AtkTextNode* ItemNameTextNode;

        [FieldOffset(0x10)]
        public AtkResNode* BackgroundResNode;

        [FieldOffset(0x18)]
        public AtkComponentIcon* IconComponent;

        [FieldOffset(0x20)]
        public AtkCollisionNode* HoverCollisionNode;

        [FieldOffset(0x28)]
        public bool Visible;

        [FieldOffset(0x29)]
        public bool IsStellarOpportunity;

        [FieldOffset(0x30)]
        public CStringPointer ItemName;

        [FieldOffset(0x38)]
        public uint iconId;

        [FieldOffset(0x3C)]
        public uint itemCount;

        [FieldOffset(0x40)]
        public uint itemId;

        [FieldOffset(0x44)]
        private uint Unk1;

        [FieldOffset(0x48)]
        public uint Index;
    }
}
