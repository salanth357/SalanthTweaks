using System.Runtime.InteropServices;

namespace SalanthTweaks.Structs;

[StructLayout(LayoutKind.Explicit, Size = 48)]
public struct AgentScenarioTreeDataExtended
{
    [FieldOffset(0)]
    public ushort CurrentScenarioQuest;
    [FieldOffset(6)]
    public ushort CompleteScenarioQuest;
    [FieldOffset(8)]
    public ushort CurrentJobQuest;
    [FieldOffset(10)]
    public ushort CurrentTipQuest;

    [FieldOffset(0x10)]
    public byte TreeTipRowID;
    
    [FieldOffset(0x13)]
    public byte IsChocoboOrCrystalTower;
    [FieldOffset(0x14)]
    public byte Field14;
    [FieldOffset(0x15)]
    public byte QuestCalculationFinished;

    [FieldOffset(0x25)]
    public byte State;

    [FieldOffset(0x2E)]
    public byte ShouldSwitchToDrawMode1;
}
