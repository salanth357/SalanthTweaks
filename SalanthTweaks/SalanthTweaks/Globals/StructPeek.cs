namespace SalanthTweaks.Globals;

public static class StructPeek
{
    public static unsafe TF GetFieldOffset<T, TF>(T* obj, uint fieldOffset) where T : unmanaged where TF : unmanaged
    {
        return *(TF*)((IntPtr)obj + fieldOffset);
    }

    private static uint BaseNodeId = ((uint)'S' << 24) | ((uint)'L' << 16);  
    public static uint MakeNodeId(uint id) => id | BaseNodeId;
}
