using System;
using System.Runtime;
using System.Runtime.CompilerServices;
using Cosmos.Boot.Limine;
using EarlyBird;
using EarlyBird.Conversion;
using EarlyBird.Internal;
using EarlyBird.String;
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



    class TestClass
    {
        public int x;
        public TestClass()
        {
            x = 0;
        }
    }
    static TestClass obj;
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int TestAllocation()
    {
        obj = new TestClass();  // This should call RhpNewFast internally
        obj.x = 42;
        return obj.x; // Return the value to ensure the object is not optimized away
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static ushort TestMethod(byte a, byte b)
    {
       return (ushort)(a + b);
    }

    [RuntimeImport("RhpAssignRef")]
    static extern void RhpAssignRef(void* target, object value);

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
        // if (TestAllocation() == 42)
        // {
        //     Serial.WriteString("Test allocation successful.");
        // }
        // else
        // {
        //     Serial.WriteString("Test allocation failed.");
        // }

        //throw new VException("This is a test exception");
        // fixed (char* p = test)
        // {

        //     Span<char> testSpan = new Span<char>();
        //     debugCatch();
        //     testSpan = new Span<char>(p, test.Length);
        //     char a = testSpan[0];
        //     Canvas.DrawChar(a, 0, 0, Color.White);

        // }
        int here = 0;
        object obj = (object)here;
        int there = 5;
        RhpAssignRef(&obj, there);
        if ((int)obj == 5)
        {
            Serial.WriteString("RhpAssignRef test successful.");
        }
        else
        {
            Serial.WriteString("RhpAssignRef test failed.");
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
