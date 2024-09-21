using System.Drawing;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;

namespace super_powers_plugin;

#region CCheckTransmitInfo
[StructLayout(LayoutKind.Sequential)]
public struct CCheckTransmitInfo
{
    public CFixedBitVecBase m_pTransmitEntity;
};

[StructLayout(LayoutKind.Sequential)]
public unsafe struct CFixedBitVecBase
{
    private const int LOG2_BITS_PER_INT = 5;
    private const int MAX_EDICT_BITS = 14;
    private const int BITS_PER_INT = 32;
    private const int MAX_EDICTS = 1 << MAX_EDICT_BITS;

    private uint* m_Ints;

    public void Clear(int bitNum)
    {
        if (!(bitNum >= 0 && bitNum < MAX_EDICTS))
            return;

        uint* pInt = m_Ints + BitVec_Int(bitNum);
        *pInt &= ~(uint)BitVec_Bit(bitNum);
    }

    public bool IsBitSet(int bitNum)
    {
        if (!(bitNum >= 0 && bitNum < MAX_EDICTS))
            return false;

        uint* pInt = m_Ints + BitVec_Int(bitNum);
        return (*pInt & BitVec_Bit(bitNum)) != 0;
    }

    private int BitVec_Int(int bitNum) => bitNum >> LOG2_BITS_PER_INT;
    private int BitVec_Bit(int bitNum) => 1 << ((bitNum) & (BITS_PER_INT - 1));
}
#endregion

[StructLayout(LayoutKind.Sequential)]
struct CUtlMemory
{
    public unsafe nint* m_pMemory;
    public int m_nAllocationCount;
    public int m_nGrowSize;
}

[StructLayout(LayoutKind.Sequential)]
struct CUtlVector
{
    public unsafe nint this[int index]
    {
        get => this.m_Memory.m_pMemory[index];
        set => this.m_Memory.m_pMemory[index] = value;
    }

    public int m_iSize;
    public CUtlMemory m_Memory;

    public nint Element(int index) => this[index];
}

class INetworkServerService : NativeObject
{
    private readonly VirtualFunctionWithReturn<nint, nint> GetIGameServerFunc;

    public INetworkServerService() : base(NativeAPI.GetValveInterface(0, "NetworkServerService_001"))
    {
        this.GetIGameServerFunc = new VirtualFunctionWithReturn<nint, nint>(this.Handle, GameData.GetOffset("INetworkServerService_GetIGameServer"));
    }

    public INetworkGameServer GetIGameServer()
    {
        return new INetworkGameServer(this.GetIGameServerFunc.Invoke(this.Handle));
    }
}

public class INetworkGameServer : NativeObject
{
    private static int SlotsOffset = GameData.GetOffset("INetworkGameServer_Slots");

    private CUtlVector Slots;

    public INetworkGameServer(nint ptr) : base(ptr)
    {
        this.Slots = Marshal.PtrToStructure<CUtlVector>(base.Handle + SlotsOffset);
    }

    public CServerSideClient? GetClientBySlot(int playerSlot)
    {
        if (playerSlot >= 0 && playerSlot < this.Slots.m_iSize)
            return this.Slots[playerSlot] == IntPtr.Zero ? null : new CServerSideClient(this.Slots[playerSlot]);

        return null;
    }
}

public class CServerSideClient : NativeObject
{
    private static int m_nForceWaitForTick = GameData.GetOffset("CServerSideClient_m_nForceWaitForTick");

    public unsafe int ForceWaitForTick
    {
        get { return *(int*)(base.Handle + m_nForceWaitForTick); }
        set { *(int*)(base.Handle + m_nForceWaitForTick) = value; }
    }

    public CServerSideClient(nint ptr) : base(ptr)
    { }

    public void ForceFullUpdate()
    {
        this.ForceWaitForTick = -1;
    }
}

