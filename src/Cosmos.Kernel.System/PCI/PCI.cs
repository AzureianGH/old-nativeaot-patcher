using Cosmos.Kernel.HAL.X64;

namespace Cosmos.Kernel.System.PCI;



public static class PCI
{
    static X64PortIO x64PortIO = new X64PortIO();
    public static ushort ConfigReadWord(byte bus, byte slot, byte func, byte offset)
    {
        uint address;
        uint lbus = (uint)bus;
        uint lslot = (uint)slot;
        uint lfunc = (uint)func;
        ushort tmp = 0;

        address = (uint)((lbus << 16) | (lslot << 11) | (lfunc << 8) | (offset & 0xFC) | ((uint)0x80000000));


        x64PortIO.WriteDWord(0xCF8, address);

        tmp = (ushort)((x64PortIO.ReadDWord(0xCFC) >> ((offset & 2) * 8)) & 0xFFFF);

        return tmp;
    }

    public static ushort CheckVendor(byte bus, byte slot)
    {
        ushort vendor, device;
        vendor = ConfigReadWord(bus, slot, 0, 0);
        device = ConfigReadWord(bus, slot, 0, 2);
        if ((vendor == 0xFFFF) || (vendor == 0x0000)) // No device found
        {
            return 0;
        }
        else
        {
            return vendor;
        }
    }

    
}