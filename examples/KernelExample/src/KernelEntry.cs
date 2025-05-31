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

        throw new IndexOutOfRangeException();

        string meow = "Hello, Cosmos!";
        char[] chararray = meow.ToCharArray();


        for (int i = 0; i < chararray.Length; i++)
        {
            Serial.ComWrite((byte)chararray[i]);
        }

        while (true)
        {
            currentTime = RTC.GetTime();
            Canvas.ClearScreen(Color.Black);
            Canvas.DrawString(currentTime, 0, 0, Color.White);
            Canvas.DrawString(currentDate, 0, 28, Color.White);


            MemoryOp.Free(currentTime);
            for (ulong i = 0; i < 0xFFFFFF; i++) ;
        }
    }
}
