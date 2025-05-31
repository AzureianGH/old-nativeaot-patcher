using System;
using System.EarlyBird.Internal.Rhp;
using System.Runtime;
using System.Runtime.CompilerServices;
using Cosmos.Boot.Limine;
using EarlyBird;
using EarlyBird.Conversion;
using EarlyBird.Internal;
using EarlyBird.String;
using Internal.Runtime;
using static EarlyBird.Graphics;

unsafe class Program
{
    static readonly LimineFramebufferRequest Framebuffer = new();
    static readonly LimineHHDMRequest HHDM = new();

    [RuntimeExport("debugCatch")]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int debugCatch()
    {
        return 0xAA;
    }

    // Internal call
    [MethodImpl(MethodImplOptions.InternalCall)]
    [RuntimeImport("*", "test_gcc_function")]
    public static extern void TestGccFunction();

    [RuntimeExport("kmainCSharp")]
    static void Main()
    {

        MemoryOp.InitializeHeap(HHDM.Offset, ulong.MaxValue);
        Canvas.Initialize((uint*)Framebuffer.Response->Framebuffers[0]->Address, (uint)Framebuffer.Response->Framebuffers[0]->Width, (uint)Framebuffer.Response->Framebuffers[0]->Height, (uint)Framebuffer.Response->Framebuffers[0]->Pitch);
        Serial.ComInit();
        RTC.Initialize();
        RTC.SetTimeZone(RTC.TimeZone.EST);
        RTC.SetDaylightSaving(RTC.DaylightSaving.Daylight);

        char* currentDate = RTC.GetDate();
        char* currentTime = RTC.GetTime();
        debugCatch();

        TestGccFunction();

        while (true) ;
        
    }
}
