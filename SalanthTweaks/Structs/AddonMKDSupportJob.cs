using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.Interop;

namespace SalanthTweaks.Structs;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
[System.Runtime.CompilerServices.InlineArrayAttribute(4)]
internal struct FixedSizeArray4<T> where T : unmanaged
{
    private T _element0;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
[System.Runtime.CompilerServices.InlineArrayAttribute(7)]
internal struct FixedSizeArray7<T> where T : unmanaged
{
    private T _element0;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
[System.Runtime.CompilerServices.InlineArrayAttribute(8)]
internal struct FixedSizeArray8<T> where T : unmanaged
{
    private T _element0;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
[System.Runtime.CompilerServices.InlineArrayAttribute(11)]
internal struct FixedSizeArray11<T> where T : unmanaged
{
    private T _element0;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
[System.Runtime.CompilerServices.InlineArrayAttribute(20)]
internal struct FixedSizeArray20<T> where T : unmanaged
{
    private T _element0;
}

[StructLayout(LayoutKind.Explicit, Size = 0xC50)]
public unsafe struct AddonMKDSupportJob
{
    [FieldOffset(0x0)]
    public AtkUnitBase Base;

    [FieldOffset(0x2f0)]
    internal FixedSizeArray4<Row> _rows;

    [System.Diagnostics.CodeAnalysis.UnscopedRefAttribute]
    public Span<Row> Rows => _rows;


    [StructLayout(LayoutKind.Explicit, Size = 0x238)]
    public struct Row
    {
        [FieldOffset(0x0)]
        internal FixedSizeArray7<MKDSupportJobNodePtrs> _jobs;

        [System.Diagnostics.CodeAnalysis.UnscopedRefAttribute]
        public Span<MKDSupportJobNodePtrs> Jobs => _jobs;

        [FieldOffset(0x230)]
        public byte numJobs;
    }

    [StructLayout(LayoutKind.Explicit, Size = 0x50)]
    public struct MKDSupportJobNodePtrs
    {
        [FieldOffset(0)]
        public AtkComponentButton* Button;

        [FieldOffset(0x8)]
        internal FixedSizeArray8<Pointer<AtkResNode>> _nodes;

        [System.Diagnostics.CodeAnalysis.UnscopedRefAttribute]
        public Span<Pointer<AtkResNode>> Nodes => _nodes;

        [FieldOffset(0x48)]
        public byte JobId;
    }
}
