using System.Runtime.InteropServices;
using Cosmos.Kernel.Boot.Limine;
using Cosmos.Kernel.Core.Memory;
using Cosmos.Kernel.System.ACPI;
using Cosmos.Kernel.System.IO;
using _ACPI = Cosmos.Kernel.System.ACPI.ACPI;

namespace Cosmos.Kernel.System.PCI;

public static partial class PCIeIds
{
    [LibraryImport("*", EntryPoint = "getPcieIds")]
    public static unsafe partial PCIeIDFile* GetPCIeIDs();

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct PCIeIDFile
    {
        public byte* start;
        public ulong size;
    }
}

public static unsafe class PCIe
{
    static bool _isInitialized = false;
    static bool _consoleOutput = false;
    static PCIeIds.PCIeIDFile* _pciIds = null;
    static PCIeIDParser parser = null;

    public unsafe class PCIeIDParser
    {
        public unsafe struct PCIeDevice
        {
            public ushort DeviceID;
            public char* DeviceName;
            public byte SubDeviceCount;
            public PCIeDevice* SubDevices;
        }

        public unsafe struct PCIeVendor
        {
            public ushort VendorID;
            public char* VendorName;
            public uint DeviceCount;
            public PCIeDevice* Devices;
        }

        private ulong _vendorCount;
        private PCIeVendor* _vendors;

        public PCIeIDParser(PCIeIds.PCIeIDFile* pcieIdFile)
        {
            _vendorCount = 0;
            _vendors = null;
            Parse(pcieIdFile);
        }

        public void Parse(PCIeIds.PCIeIDFile* pcieIdFile)
        {
            DisplayWriteLine("PCIe: Parsing PCI IDs...");

            if (pcieIdFile == null || pcieIdFile->start == null || pcieIdFile->size == 0)
            {
                DisplayWriteLine("PCIe: Invalid PCI ID file.");
                return;
            }

            byte* ptr = pcieIdFile->start;
            ulong fileSize = pcieIdFile->size;
            ulong index = 0;

            PCIeVendor* currentVendor = null;
            PCIeDevice* currentDevice = null;

            while (index < fileSize)
            {
                // --- Read a line ---
                byte* lineStart = ptr + index;
                ulong lineLength = 0;

                while (
                    index + lineLength < fileSize
                    && *(ptr + index + lineLength) != 0x0A
                    && *(ptr + index + lineLength) != 0x0D
                )
                {
                    lineLength++;
                }

                // Move index to the next line (skip newline characters)
                index += lineLength;
                while (index < fileSize && (*(ptr + index) == 0x0A || *(ptr + index) == 0x0D))
                    index++;

                if (lineLength == 0)
                    continue; // skip empty lines
                if (*lineStart == '#')
                    continue; // skip comments

                // --- Determine indentation ---
                byte indentLevel = 0;
                while (indentLevel < lineLength && *(lineStart + indentLevel) == 0x09)
                    indentLevel++;

                byte* contentStart = lineStart + indentLevel;
                ulong contentLength = lineLength - indentLevel;

                if (indentLevel == 0)
                {
                    // Vendor line (4 hex digits + space + name)
                    if (contentLength < 5)
                        continue;

                    ushort vendorID = 0;
                    for (int i = 0; i < 4; i++)
                    {
                        byte c = *(contentStart + i);
                        vendorID <<= 4;
                        if (c >= '0' && c <= '9')
                            vendorID |= (ushort)(c - '0');
                        else if (c >= 'a' && c <= 'f')
                            vendorID |= (ushort)(c - 'a' + 10);
                        else if (c >= 'A' && c <= 'F')
                            vendorID |= (ushort)(c - 'A' + 10);
                        else
                            vendorID = 0;
                    }

                    if (vendorID == 0)
                        continue;

                    // Copy vendor name to null-terminated buffer
                    ulong nameLength = contentLength - 5;
                    byte* vendorName = (byte*)MemoryOp.Alloc((uint)(nameLength + 1));
                    for (ulong i = 0; i < nameLength; i++)
                        vendorName[i] = *(contentStart + 5 + i);
                    vendorName[nameLength] = 0;

                    // Allocate vendor array
                    PCIeVendor* newVendors = (PCIeVendor*)
                        MemoryOp.ReAlloc(
                            _vendors,
                            (uint)((uint)(_vendorCount + 1) * sizeof(PCIeVendor))
                        );
                    if (newVendors == null)
                    {
                        DisplayWriteLine("PCIe: Failed to allocate memory for vendors.");
                        return;
                    }
                    _vendors = newVendors;

                    currentVendor = &_vendors[_vendorCount];
                    currentVendor->VendorID = vendorID;
                    currentVendor->VendorName = (char*)vendorName;
                    currentVendor->DeviceCount = 0;
                    currentVendor->Devices = null;

                    _vendorCount++;
                    currentDevice = null;
                }
                else if (indentLevel == 1 && currentVendor != null)
                {
                    // Device line (4 hex digits + space + name)
                    if (contentLength < 5)
                        continue;

                    ushort deviceID = 0;
                    for (int i = 0; i < 4; i++)
                    {
                        byte c = *(contentStart + i);
                        deviceID <<= 4;
                        if (c >= '0' && c <= '9')
                            deviceID |= (ushort)(c - '0');
                        else if (c >= 'a' && c <= 'f')
                            deviceID |= (ushort)(c - 'a' + 10);
                        else if (c >= 'A' && c <= 'F')
                            deviceID |= (ushort)(c - 'A' + 10);
                        else
                            deviceID = 0;
                    }
                    if (deviceID == 0)
                        continue;

                    ulong nameLength = contentLength - 5;
                    byte* deviceName = (byte*)MemoryOp.Alloc((uint)(nameLength + 1));
                    for (ulong i = 0; i < nameLength; i++)
                        deviceName[i] = *(contentStart + 5 + i);
                    deviceName[nameLength] = 0;

                    PCIeDevice* newDevices = (PCIeDevice*)
                        MemoryOp.ReAlloc(
                            currentVendor->Devices,
                            (uint)((currentVendor->DeviceCount + 1) * sizeof(PCIeDevice))
                        );
                    if (newDevices == null)
                    {
                        DisplayWriteLine("PCIe: Failed to allocate memory for devices.");
                        continue;
                    }
                    currentVendor->Devices = newDevices;

                    currentDevice = &currentVendor->Devices[currentVendor->DeviceCount];
                    currentDevice->DeviceID = deviceID;
                    currentDevice->DeviceName = (char*)deviceName;
                    currentDevice->SubDeviceCount = 0;
                    currentDevice->SubDevices = null;

                    currentVendor->DeviceCount++;
                }
                else if (indentLevel == 2 && currentDevice != null)
                {
                    // Subdevice line (VendorID DeviceID + space + name)
                    if (contentLength < 9)
                        continue;

                    ushort subVendorID = 0;
                    ushort subDeviceID = 0;

                    for (int i = 0; i < 4; i++)
                    {
                        byte c = *(contentStart + i);
                        subVendorID <<= 4;
                        if (c >= '0' && c <= '9')
                            subVendorID |= (ushort)(c - '0');
                        else if (c >= 'a' && c <= 'f')
                            subVendorID |= (ushort)(c - 'a' + 10);
                        else if (c >= 'A' && c <= 'F')
                            subVendorID |= (ushort)(c - 'A' + 10);
                    }
                    for (int i = 4; i < 8; i++)
                    {
                        byte c = *(contentStart + i);
                        subDeviceID <<= 4;
                        if (c >= '0' && c <= '9')
                            subDeviceID |= (ushort)(c - '0');
                        else if (c >= 'a' && c <= 'f')
                            subDeviceID |= (ushort)(c - 'a' + 10);
                        else if (c >= 'A' && c <= 'F')
                            subDeviceID |= (ushort)(c - 'A' + 10);
                    }

                    ulong nameLength = contentLength - 9;
                    byte* subDeviceName = (byte*)MemoryOp.Alloc((uint)(nameLength + 1));
                    for (ulong i = 0; i < nameLength; i++)
                        subDeviceName[i] = *(contentStart + 9 + i);
                    subDeviceName[nameLength] = 0;

                    PCIeDevice* newSubDevices = (PCIeDevice*)
                        MemoryOp.ReAlloc(
                            currentDevice->SubDevices,
                            (uint)((currentDevice->SubDeviceCount + 1) * sizeof(PCIeDevice))
                        );
                    if (newSubDevices == null)
                    {
                        DisplayWriteLine("PCIe: Failed to allocate memory for subdevices.");
                        continue;
                    }
                    currentDevice->SubDevices = newSubDevices;

                    PCIeDevice* subDevice = &currentDevice->SubDevices[
                        currentDevice->SubDeviceCount
                    ];
                    subDevice->DeviceID = subDeviceID;
                    subDevice->DeviceName = (char*)subDeviceName;
                    subDevice->SubDeviceCount = 0;
                    subDevice->SubDevices = null;

                    currentDevice->SubDeviceCount++;
                }
            }

            DisplayWriteLine("PCIe: Parsing completed.");
        }

        public ulong VendorCount => _vendorCount;
        public PCIeVendor* Vendors => _vendors;
    }

    public static bool Initialize(bool consoleOutput = false)
    {
        _consoleOutput = consoleOutput;

        if (_isInitialized)
        {
            return true;
        }

        if (_ACPI.MCFGEntries.Entries == null || _ACPI.MCFGEntries.Entries.Length == 0)
        {
            DisplayWriteLine("PCIe: No MCFG entries found.");
            return false;
        }
        DisplayWriteLine("PCIe: MCFG entries found.");

        DisplayWriteLine("PCIe: Loading PCI IDs...");

        _pciIds = PCIeIds.GetPCIeIDs();

        if (_pciIds == null)
        {
            DisplayWriteLine("PCIe: Failed to load PCI IDs.");
            return false;
        }

        DisplayWrite("PCIe: PCI IDs loaded with size: ");
        DisplayWrite(_pciIds->size);
        DisplayWriteLine(" bytes.");

        parser = new PCIeIDParser(_pciIds);

        DisplayWriteLine("PCIe: Built PCIe ID tree.");

        _isInitialized = true;
        return true;
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
