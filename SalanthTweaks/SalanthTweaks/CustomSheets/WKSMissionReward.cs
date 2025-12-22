using Lumina.Excel;
using Lumina.Excel.Sheets;

namespace SalanthTweaks.CustomSheets;

[Sheet("WKSMissionReward", 0x426EC072)]
public readonly unsafe struct WKSMissionReward(ExcelPage page, uint offset, uint row) : IExcelRow<WKSMissionReward>
{
    public uint RowOffset => offset;

    public ExcelPage ExcelPage => page;

    public uint RowId => row;

    public readonly RowRef<Item> Item => new(page.Module, page.ReadUInt32(offset), page.Language);
    public readonly Collection<ushort> ExpModifier => new(page, offset, offset, &ExpModifierCtor, size: 3);

    public readonly ushort CosmoCredits => page.ReadUInt16(offset + 10);
    public readonly ushort PlanetCredits => page.ReadUInt16(offset + 12);
    public readonly Collection<ushort> ResearchReward => new(page, offset, offset, &ResearchRewardCtor, size: 3);
    public readonly ushort ItemCount => page.ReadUInt16(offset + 20);
    public readonly byte Unknown19 => page.ReadUInt8(offset + 22);
    public readonly byte Unknown9 => page.ReadUInt8(offset + 23);
    public readonly byte Unknown10 => page.ReadUInt8(offset + 24);
    public readonly byte Unknown11 => page.ReadUInt8(offset + 25);

    public readonly Collection<byte> TypeIndex => new(page, offset, offset, &TypeIndexCtor, size: 3);

    private static ushort ExpModifierCtor(ExcelPage page, uint parentOffset, uint offset, uint i) =>
        page.ReadUInt16(offset + 4 + i * 2);

    private static ushort ResearchRewardCtor(ExcelPage page, uint parentOffset, uint offset, uint i) =>
        page.ReadUInt16(offset + 14 + i * 2);

    private static byte TypeIndexCtor(ExcelPage page, uint parentOffset, uint offset, uint i) =>
        page.ReadUInt8(offset + 26 + i);

    static WKSMissionReward IExcelRow<WKSMissionReward>.Create(ExcelPage page, uint offset, uint row) =>
        new(page, offset, row);
}
