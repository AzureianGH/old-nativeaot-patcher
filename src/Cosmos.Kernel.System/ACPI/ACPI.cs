using Cosmos.Kernel.Boot.Limine;
using Cosmos.Kernel.System.IO;
using Cosmos.Kernel.Core.Memory;
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

    public unsafe struct RSDP
    {
        public fixed byte Signature[8]; // "RSD PTR "
        public byte Checksum;           // Checksum of first 20 bytes
        public fixed byte OEMID[6];     // OEM Identifier
        public byte Revision;           // 0 for ACPI 1.0, 2 for ACPI 2.0+
        public uint RsdtAddress;        // 32-bit physical address of RSDT
        // Fields below are available in ACPI 2.0+
        public uint Length;             // Total length of the RSDP structure
        public ulong XsdtAddress;       // 64-bit physical address of XSDT
        public byte ExtendedChecksum;   // Checksum of entire table
        public fixed byte Reserved[3];  // Reserved, must be zero
    }

    public unsafe struct ACPISDTHeader
    {
        public fixed byte Signature[4]; // e.g., "RSDT", "XSDT"
        public uint Length;             // Length of the table, including this header
        public byte Revision;           // Revision of the structure
        public byte Checksum;           // Entire table must sum to zero
        public fixed byte OEMID[6];     // OEM Identifier
        public fixed byte OEMTableID[8];// OEM Table Identifier
        public uint OEMRevision;        // OEM Revision number
        public uint CreatorID;          // Vendor ID of utility that created the table
        public uint CreatorRevision;    // Revision of utility that created the table
    }

    public static unsafe bool Initialize()
    {
        if (_initialized)
        {
            return true;
        }

        var rsdpResponse = Limine.RSDP.Response;
        if (rsdpResponse == null || rsdpResponse->Address == 0)
        {
            Serial.WriteString("ACPI: RSDP not found.\n\r");
            return false;
        }

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
            Serial.WriteString("ACPI: Root table address missing.\n\r");
            return false;
        }

        _rootTable = (ACPISDTHeader*)(rootPhysical);
        if (_rootTable == null)
        {
            Serial.WriteString("ACPI: Failed to map root table.\n\r");
            return false;
        }

        if (!ValidateChecksum((byte*)_rootTable, _rootTable->Length))
        {
            Serial.WriteString("ACPI: Root table checksum invalid.\n\r");
            return false;
        }

        uint headerSize = (uint)sizeof(ACPISDTHeader);
        if (_rootTable->Length <= headerSize)
        {
            Serial.WriteString("ACPI: Root table has no entries.\n\r");
            return false;
        }

        uint entriesLength = _rootTable->Length - headerSize;
        uint entrySize = _isXsdt ? 8u : 4u;
        int entryCount = (int)(entriesLength / entrySize);
        if (entryCount <= 0)
        {
            Serial.WriteString("ACPI: No tables discovered.\n\r");
            return false;
        }

        _tablePointers = (ulong*)MemoryOp.Alloc((uint)(entryCount * sizeof(ulong)));
        if (_tablePointers == null)
        {
            Serial.WriteString("ACPI: Failed to allocate table list.\n\r");
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
        Serial.WriteString("ACPI: Initialized.\n\r");
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
            Serial.WriteString("ACPI: DumpTables failed to initialize.\n\r");
            return;
        }
        
        if (!display) Serial.WriteString("ACPI Table Dump:\n\r");
        else Console.WriteLine("ACPI Table Dump:");

        for (int i = 0; i < _tableCount; i++)
        {
            ulong tablePtr = _tablePointers[i];
            if (!display) Serial.WriteString(" - ");
            else Console.Write(" - ");

            if (tablePtr == 0)
            {
                if (!display) Serial.WriteString("<null>\n\r");
                else Console.WriteLine("<null>");
                continue;
            }

            ACPISDTHeader* header = (ACPISDTHeader*)tablePtr;
            WriteSignature(header->Signature, display);
            if (!display)
            {
                Serial.WriteString(" @ 0x");
                Serial.WriteHex(tablePtr);
                Serial.WriteString(" len=");
                Serial.WriteNumber(header->Length);
                Serial.WriteString("\n\r");
            }
            else
            {
                Console.Write(" @ 0x");
                Console.Write(tablePtr.ToString("X"));
                Console.Write(" len=");
                Console.WriteLine(header->Length);
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
                if (!display) Serial.ComWrite((byte)'.');
                else Console.Write('.');
            }
            else
            {
                if (!display) Serial.ComWrite(value);
                else Console.Write((char)value);
            }
        }
    }
}