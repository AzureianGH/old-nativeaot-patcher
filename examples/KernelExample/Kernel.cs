using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Cosmos.Build.API.Attributes;
using Cosmos.Build.API.Enum;
using Cosmos.Kernel.Boot.Limine;
using Cosmos.Kernel.Core.Memory;
using Cosmos.Kernel.Core.Runtime;
using Cosmos.Kernel.HAL;
using Cosmos.Kernel.System.ACPI;
using Cosmos.Kernel.System.Graphics;
using Cosmos.Kernel.System.Graphics.Fonts;
using Cosmos.Kernel.System.IO;
using PlatformArchitecture = Cosmos.Build.API.Enum.PlatformArchitecture;
using Cosmos.Kernel.System.PCI;
internal static unsafe partial class Program
{
    [LibraryImport("test", EntryPoint = "testGCC")]
    [return: MarshalUsing(typeof(SimpleStringMarshaler))]
    public static unsafe partial string testGCC();

    [UnmanagedCallersOnly(EntryPoint = "__managed__Main")]
    private static void KernelMain() => Main();

    private static void Main()
    {
        KernelConsole.Initialize();

        bool acpiInit = ACPI.Initialize(true);
        if (acpiInit)
        {
            Console.WriteLine("ACPI initialized successfully.");

            bool pcieInit = PCIe.Initialize(true);
            if (pcieInit)
            {
                Console.WriteLine("PCIe initialized successfully.");
            }
            else
            {
                Console.WriteLine("PCIe initialization failed.");
            }
        }
        else
        {
            Console.WriteLine("ACPI initialization failed.");
        }

        Console.WriteLine("Hello Cosmos World from x64!");

        while (true);
    }
}

[CustomMarshaller(typeof(string), MarshalMode.Default, typeof(SimpleStringMarshaler))]
internal static unsafe class SimpleStringMarshaler
{
    public static string ConvertToManaged(char* unmanaged)
    {
        string result = new(unmanaged);

        return result;
    }

    public static char* ConvertToUnmanaged(string managed)
    {
        fixed (char* p = managed)
        {
            return p;
        }
    }
}
