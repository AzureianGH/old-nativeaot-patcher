using System.Runtime.InteropServices;
using Cosmos.Kernel.Boot.Limine;
using Cosmos.Kernel.Core.Memory;
using Cosmos.Kernel.System.IO;

namespace Cosmos.Kernel.System.ACPI;

public static unsafe class ACPI
{
    static bool _initialized;
    static bool _isXsdt = false;
    static ulong _rsdtAddress = 0;
    static ulong _xsdtAddress = 0; // if applicable
    static ulong* _tablePointers;
    static int _tableCount;
    static ACPISDTHeader* _rootTable;
    static bool _consoleOutput = false;
    static FADT* _fadt;
    static MCFG* _mcfg;
    public static MCFGContainer MCFGEntries;

    public unsafe struct RSDP
    {
        public fixed byte Signature[8]; // "RSD PTR "
        public byte Checksum; // Checksum of first 20 bytes
        public fixed byte OEMID[6]; // OEM Identifier
        public byte Revision; // 0 for ACPI 1.0, 2 for ACPI 2.0+
        public uint RsdtAddress; // 32-bit physical address of RSDT

        // Fields below are available in ACPI 2.0+
        public uint Length; // Total length of the RSDP structure
        public ulong XsdtAddress; // 64-bit physical address of XSDT
        public byte ExtendedChecksum; // Checksum of entire table
        public fixed byte Reserved[3]; // Reserved, must be zero
    }

    public unsafe struct ACPISDTHeader
    {
        public fixed byte Signature[4]; // e.g., "RSDT", "XSDT"
        public uint Length; // Length of the table, including this header
        public byte Revision; // Revision of the structure
        public byte Checksum; // Entire table must sum to zero
        public fixed byte OEMID[6]; // OEM Identifier
        public fixed byte OEMTableID[8]; // OEM Table Identifier
        public uint OEMRevision; // OEM Revision number
        public uint CreatorID; // Vendor ID of utility that created the table
        public uint CreatorRevision; // Revision of utility that created the table
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct GenericAddressStructure
    {
        public byte AddressSpace;
        public byte BitWidth;
        public byte BitOffset;
        public byte AccessSize;
        public ulong Address;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct FADT
    {
        public ACPISDTHeader Header;
        public uint FirmwareCtrl;
        public uint DSDT;
        public byte Reserved;
        public byte PreferredPowerManagementProfile;
        public ushort SCIInterrupt;
        public uint SMICommandPort;
        public byte AcpiEnable;
        public byte AcpiDisable;
        public byte S4BiosReq;
        public byte PSTATEControl;
        public uint PM1aEventBlock;
        public uint PM1bEventBlock;
        public uint PM1aControlBlock;
        public uint PM1bControlBlock;
        public uint PM2ControlBlock;
        public uint PMTimerBlock;
        public uint GPE0Block;
        public uint GPE1Block;
        public byte PM1EventLength;
        public byte PM1ControlLength;
        public byte PM2ControlLength;
        public byte PMTimerLength;
        public byte GPE0Length;
        public byte GPE1Length;
        public byte GPE1Base;
        public byte CStateControl;
        public ushort WorstC2Latency;
        public ushort WorstC3Latency;
        public ushort FlushSize;
        public ushort FlushStride;
        public byte DutyOffset;
        public byte DutyWidth;
        public byte DayAlarm;
        public byte MonthAlarm;
        public byte Century;

        public ushort BootArchitectureFlags;
        public byte Reserved2;
        public uint Flags;
        public GenericAddressStructure ResetReg;
        public byte ResetValue;
        public fixed byte Reserved3[3];
        public ulong XFirmwareCtrl;
        public ulong XDSDT;

        public GenericAddressStructure XPm1aEventBlock;
        public GenericAddressStructure XPm1bEventBlock;
        public GenericAddressStructure XPm1aControlBlock;
        public GenericAddressStructure XPm1bControlBlock;
        public GenericAddressStructure XPm2ControlBlock;
        public GenericAddressStructure XPMTimerBlock;
        public GenericAddressStructure XGPE0Block;
        public GenericAddressStructure XGPE1Block;
    }

    public enum PreferredPowerManagementProfileType
    {
        Unspecified = 0,
        Desktop = 1,
        Mobile = 2,
        Workstation = 3,
        EnterpriseServer = 4,
        SOHOServer = 5,
        AppliancePC = 6,
        PerformanceServer = 7,
    }

    public enum AddressSpaceType
    {
        SystemMemory = 0,
        SystemIO = 1,
        PCIConfigurationSpace = 2,
        EmbeddedController = 3,
        SystemManagementBus = 4,
        SystemCMOS = 5,
        PCIBarTarget = 6,
        IPMI = 7, // Intelligent Platform Management Interface
        GeneralPurposeIO = 8,
        GenericSerialBus = 9,
        PlatformCommunicationsChannel = 0x0A,
    }

    public enum AccessSizeType
    {
        Undefined = 0,
        ByteAccess = 1,
        WordAccess = 2,
        DWordAccess = 3,
        QWordAccess = 4,
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct MCFGEntry
    {
        public ulong BaseAddress;
        public ushort PCIGroup;
        public byte StartBus;
        public byte EndBus;
        public uint Reserved;
    }

    public struct MCFGContainer
    {
        public MCFGEntry[] Entries;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct MCFG
    {
        public ACPISDTHeader Header;
        public ulong Reserved;
        public MCFGEntry Entry; // Variable length array of MCFGEntry
    }

    public static string GetPreferredPowerManagementProfileName(byte profile)
    {
        return profile switch
        {
            0 => "Unspecified",
            1 => "Desktop",
            2 => "Mobile",
            3 => "Workstation",
            4 => "Enterprise Server",
            5 => "SOHO Server",
            6 => "Appliance PC",
            7 => "Performance Server",
            _ => "Reserved/Unknown",
        };
    }

    public static string GetAccessSizeTypeName(byte size)
    {
        return size switch
        {
            0 => "Undefined",
            1 => "Byte Access",
            2 => "Word Access",
            3 => "DWord Access",
            4 => "QWord Access",
            _ => "Reserved/Unknown",
        };
    }

    public static string GetAddressSpaceTypeName(byte type)
    {
        return type switch
        {
            0 => "System Memory",
            1 => "System I/O",
            2 => "PCI Configuration Space",
            3 => "Embedded Controller",
            4 => "System Management Bus",
            5 => "System CMOS",
            6 => "PCI Bar Target",
            7 => "Intelligent Platform Management Interface (IPMI)",
            8 => "General Purpose I/O",
            9 => "Generic Serial Bus",
            0x0A => "Platform Communications Channel",
            _ => "Reserved/Unknown",
        };
    }

    public static unsafe bool Initialize(bool consoleOutput = false)
    {
        _consoleOutput = consoleOutput;

        if (_initialized)
        {
            return true;
        }
        DisplayWriteLine("+=================================================+");
        DisplayWriteLine("ACPI: Starting initialization...");

        var rsdpResponse = Limine.RSDP.Response;
        if (rsdpResponse == null || rsdpResponse->Address == 0)
        {
            DisplayWriteLine("ACPI: RSDP not found.");
            return false;
        }

        DisplayWrite("ACPI: RSDP found at 0x");
        DisplayWrite(rsdpResponse->Address, true);
        DisplayWriteLine();

        RSDP* rsdp = (RSDP*)(rsdpResponse->Address);
        _rsdtAddress = rsdp->RsdtAddress;
        _xsdtAddress = 0;
        _isXsdt = false;

        if (rsdp->Revision >= 2 && rsdp->XsdtAddress != 0)
        {
            _isXsdt = true;
            _xsdtAddress = rsdp->XsdtAddress;
        }

        ulong rootPhysical = _isXsdt ? _xsdtAddress : _rsdtAddress;
        if (rootPhysical == 0)
        {
            DisplayWriteLine("ACPI: Root table address missing.");
            return false;
        }

        DisplayWrite("ACPI: ");
        DisplayWrite(_isXsdt ? "XSDT" : "RSDT");
        DisplayWrite(" found at 0x");
        DisplayWrite(rootPhysical + Limine.HHDM.Offset, true);
        DisplayWriteLine();

        _rootTable = (ACPISDTHeader*)(rootPhysical);
        if (_rootTable == null)
        {
            DisplayWriteLine("ACPI: Failed to map root table.");
            return false;
        }

        DisplayWrite("ACPI: OEM ID: ");
        for (int i = 0; i < 6; i++)
        {
            byte c = _rootTable->OEMID[i];
            if (c == 0)
                break;
            DisplayWrite((char)c);
        }
        DisplayWriteLine();

        if (!ValidateChecksum((byte*)_rootTable, _rootTable->Length))
        {
            DisplayWriteLine("ACPI: Root table checksum invalid.");
            return false;
        }

        DisplayWrite("ACPI: Root table length: ");
        DisplayWrite(_rootTable->Length);
        DisplayWriteLine(" bytes");

        uint headerSize = (uint)sizeof(ACPISDTHeader);
        if (_rootTable->Length <= headerSize)
        {
            DisplayWriteLine("ACPI: Root table has no entries.");
            return false;
        }

        DisplayWriteLine("ACPI: Validating and parsing tables...");

        uint entriesLength = _rootTable->Length - headerSize;
        uint entrySize = _isXsdt ? 8u : 4u;
        int entryCount = (int)(entriesLength / entrySize);
        if (entryCount <= 0)
        {
            DisplayWriteLine("ACPI: No tables discovered.");
            return false;
        }

        _tablePointers = (ulong*)MemoryOp.Alloc((uint)(entryCount * sizeof(ulong)));
        if (_tablePointers == null)
        {
            DisplayWriteLine("ACPI: Failed to allocate table list.");
            return false;
        }

        byte* entryPtr = ((byte*)_rootTable) + headerSize;
        for (int i = 0; i < entryCount; i++)
        {
            ulong tablePhysical;
            if (_isXsdt)
            {
                tablePhysical = *((ulong*)entryPtr);
            }
            else
            {
                tablePhysical = *((uint*)entryPtr);
            }

            entryPtr += entrySize;

            if (tablePhysical == 0)
            {
                _tablePointers[i] = 0;
                continue;
            }

            ulong tableVirtual = tablePhysical;
            _tablePointers[i] = tableVirtual;
        }

        _tableCount = entryCount;
        _initialized = true;
        DisplayWrite("ACPI: Table parsing complete. ");
        DisplayWrite((ulong)_tableCount);
        DisplayWriteLine(" tables found.");

        DisplayWriteLine("ACPI: Locating FADT...");

        byte* facptext = (byte*)MemoryOp.Alloc(4);
        facptext[0] = (byte)'F';
        facptext[1] = (byte)'A';
        facptext[2] = (byte)'C';
        facptext[3] = (byte)'P';
        // Locate FADT
        ACPISDTHeader* fadtHeader = FindTable(ReadSignature(facptext));
        MemoryOp.Free(facptext);
        if (fadtHeader == null)
        {
            DisplayWriteLine("ACPI: FADT not found.");
            return false;
        }
        DisplayWrite("ACPI: FADT found at 0x");
        DisplayWriteLine((ulong)fadtHeader + Limine.HHDM.Offset, true);

        _fadt = (FADT*)fadtHeader;
        if (_fadt == null)
        {
            DisplayWriteLine("ACPI: Failed to map FADT.");
            return false;
        }
        if (!ValidateChecksum((byte*)_fadt, _fadt->Header.Length))
        {
            DisplayWriteLine("ACPI: FADT checksum invalid.");
            return false;
        }

        // Locate MCFG
        byte* mcfgtext = (byte*)MemoryOp.Alloc(4);
        mcfgtext[0] = (byte)'M';
        mcfgtext[1] = (byte)'C';
        mcfgtext[2] = (byte)'F';
        mcfgtext[3] = (byte)'G';
        ACPISDTHeader* mcfgHeader = FindTable(ReadSignature(mcfgtext));
        MemoryOp.Free(mcfgtext);
        if (mcfgHeader == null)
        {
            DisplayWriteLine("ACPI: MCFG not found.");
            return false; // MCFG is essential for PCIe enumeration
        }
        else
        {
            DisplayWrite("ACPI: MCFG found at 0x");
            DisplayWriteLine((ulong)mcfgHeader + Limine.HHDM.Offset, true);

            _mcfg = (MCFG*)mcfgHeader;
            if (_mcfg == null)
            {
                DisplayWriteLine("ACPI: Failed to map MCFG.");
                return false;
            }
            if (!ValidateChecksum((byte*)_mcfg, _mcfg->Header.Length))
            {
                DisplayWriteLine("ACPI: MCFG checksum invalid.");
                return false;
            }

            // Formulate MCFG entries
            int mcfgEntryCount =
                ((int)_mcfg->Header.Length - (int)sizeof(ACPISDTHeader) - 8) / sizeof(MCFGEntry);
            MCFGEntries = new MCFGContainer();
            MCFGEntries.Entries = new MCFGEntry[mcfgEntryCount];
            for (int i = 0; i < mcfgEntryCount; i++)
            {
                MCFGEntries.Entries[i] = *(
                    (MCFGEntry*)((byte*)_mcfg + sizeof(ACPISDTHeader) + 8 + i * sizeof(MCFGEntry))
                );
            }

            DisplayWrite("ACPI: MCFG contains ");
            DisplayWrite((ulong)mcfgEntryCount);
            DisplayWriteLine(" entries.");
        }

        DisplayWriteLine("ACPI: Initialized.");
        DisplayWriteLine("+=================================================+");
        return true;
    }

    public static unsafe ulong* GetTablePointers(out int count)
    {
        if (!_initialized && !Initialize())
        {
            count = 0;
            return null;
        }

        count = _tableCount;
        return _tablePointers;
    }

    public static unsafe ACPISDTHeader* FindTable(uint signature)
    {
        if (!_initialized && !Initialize())
        {
            DisplayWriteLine("ACPI: FindTable failed to initialize.");
            return null;
        }

        for (int i = 0; i < _tableCount; i++)
        {
            ulong tablePtr = _tablePointers[i];
            if (tablePtr == 0)
            {
                continue;
            }

            ACPISDTHeader* header = (ACPISDTHeader*)tablePtr;
            if (ReadSignature(header->Signature) == signature)
            {
                return header;
            }
        }

        return null;
    }

    public static unsafe void DumpTables(bool display = false)
    {
        if (!_initialized && !Initialize())
        {
            DisplayWriteLine("ACPI: DumpTables failed to initialize.");
            return;
        }

        DisplayWriteLine("ACPI Table Dump:");

        for (int i = 0; i < _tableCount; i++)
        {
            ulong tablePtr = _tablePointers[i];
            DisplayWrite(" - ");

            if (tablePtr == 0)
            {
                DisplayWriteLine("<null>");
                continue;
            }

            ACPISDTHeader* header = (ACPISDTHeader*)tablePtr;
            WriteSignature(header->Signature, display);

            DisplayWrite(" @ 0x");
            DisplayWrite(tablePtr + Limine.HHDM.Offset, true);
            DisplayWrite(" len=");
            DisplayWriteLine(header->Length);
            if (
                header->Signature[0] == 'F'
                && header->Signature[1] == 'A'
                && header->Signature[2] == 'C'
                && header->Signature[3] == 'P'
            )
            {
                FADT* fadt = (FADT*)header;
                DisplayWrite("   -  Preferred Power Management Profile: ");
                DisplayWriteLine(
                    GetPreferredPowerManagementProfileName(fadt->PreferredPowerManagementProfile)
                );
                DisplayWrite("   -  Address Space: ");
                DisplayWriteLine(GetAddressSpaceTypeName(fadt->XPm1aEventBlock.AddressSpace));
                DisplayWrite("   -  Access Size: ");
                DisplayWriteLine(GetAccessSizeTypeName(fadt->XPm1aEventBlock.AccessSize));
                DisplayWrite("   -  PM1a Event Block Address: 0x");
                DisplayWriteLine(fadt->PM1aEventBlock, true);
                DisplayWrite("   -  PM1b Event Block Address: 0x");
                DisplayWriteLine(fadt->PM1bEventBlock, true);
                DisplayWrite("   -  PM1a Control Block Address: 0x");
                DisplayWriteLine(fadt->PM1aControlBlock, true);
                DisplayWrite("   -  PM1b Control Block Address: 0x");
                DisplayWriteLine(fadt->PM1bControlBlock, true);
                DisplayWrite("   -  PM2 Control Block Address: 0x");
                DisplayWriteLine(fadt->PM2ControlBlock, true);
                DisplayWrite("   -  PM Timer Block Address: 0x");
                DisplayWriteLine(fadt->PMTimerBlock, true);
                DisplayWrite("   -  GPE0 Block Address: 0x");
                DisplayWriteLine(fadt->GPE0Block, true);
                DisplayWrite("   -  GPE1 Block Address: 0x");
                DisplayWriteLine(fadt->GPE1Block, true);
                DisplayWrite("   -  SCI Interrupt: ");
                DisplayWriteLine(fadt->SCIInterrupt);
            }
        }
    }

    private static unsafe bool ValidateChecksum(byte* data, uint length)
    {
        byte sum = 0;
        for (uint i = 0; i < length; i++)
        {
            sum += data[i];
        }

        return sum == 0;
    }

    private static unsafe uint ReadSignature(byte* signature)
    {
        return (uint)signature[0]
            | ((uint)signature[1] << 8)
            | ((uint)signature[2] << 16)
            | ((uint)signature[3] << 24);
    }

    private static unsafe void WriteSignature(byte* signature, bool display = false)
    {
        for (int i = 0; i < 4; i++)
        {
            byte value = signature[i];
            if (value == 0)
            {
                DisplayWrite('.');
            }
            else
            {
                DisplayWrite((char)value);
            }
        }
    }

    private static unsafe void DisplayWrite()
    {
        if (_consoleOutput)
        {
            Console.WriteLine();
        }
        else
        {
            Serial.WriteString("\n\r");
        }
    }

    private static unsafe void DisplayWrite(string text)
    {
        if (_consoleOutput)
        {
            Console.Write(text);
        }
        else
        {
            Serial.WriteString(text);
        }
    }

    private static unsafe void DisplayWrite(ulong value, bool hex = false)
    {
        if (_consoleOutput)
        {
            if (hex)
                Console.Write(value.ToString("X"));
            else
                Console.Write(value.ToString());
        }
        else
        {
            if (hex)
                Serial.WriteHex(value);
            else
                Serial.WriteNumber(value);
        }
    }

    private static unsafe void DisplayWrite(char value)
    {
        if (_consoleOutput)
        {
            Console.Write(value);
        }
        else
        {
            Serial.ComWrite((byte)value);
        }
    }

    private static unsafe void DisplayWriteLine(string value)
    {
        if (_consoleOutput)
        {
            Console.WriteLine(value);
        }
        else
        {
            Serial.WriteString(value);
            Serial.WriteString("\n\r");
        }
    }

    private static unsafe void DisplayWriteLine(ulong value, bool hex = false)
    {
        if (_consoleOutput)
        {
            if (hex)
                Console.WriteLine(value.ToString("X"));
            else
                Console.WriteLine(value.ToString());
        }
        else
        {
            if (hex)
                Serial.WriteHex(value);
            else
                Serial.WriteNumber(value);
            Serial.WriteString("\n\r");
        }
    }

    private static unsafe void DisplayWriteLine(char value)
    {
        if (_consoleOutput)
        {
            Console.WriteLine(value);
        }
        else
        {
            Serial.ComWrite((byte)value);
            Serial.WriteString("\n\r");
        }
    }

    private static unsafe void DisplayWriteLine()
    {
        if (_consoleOutput)
        {
            Console.WriteLine();
        }
        else
        {
            Serial.WriteString("\n\r");
        }
    }
}
