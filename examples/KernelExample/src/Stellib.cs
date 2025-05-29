using System;
using EarlyBird;
using EarlyBird.Conversion;
using EarlyBird.PSF;
using EarlyBird.String;
using static EarlyBird.Internal.Native;

namespace EarlyBird
{
    public class Color
    {
        public static uint Red = 0xFF0000;
        public static uint Green = 0x00FF00;
        public static uint Blue = 0x0000FF;
        public static uint White = 0xFFFFFF;
        public static uint Black = 0x000000;
        public static uint Yellow = 0xFFFF00;
        public static uint Cyan = 0x00FFFF;
        public static uint Magenta = 0xFF00FF;
        public static uint Orange = 0xFFA500;
        public static uint Purple = 0x800080;
        public static uint Pink = 0xFFC0CB;
        public static uint Brown = 0xA52A2A;
        public static uint Gray = 0x808080;
        public static uint LightGray = 0xD3D3D3;
        public static uint DarkGray = 0xA9A9A9;
        public static uint LightRed = 0xFF7F7F;
        public static uint LightGreen = 0x7FFF7F;
        public static uint LightBlue = 0x7F7FFF;
        public static uint LightYellow = 0xFFFF7F;
        public static uint LightCyan = 0x7FFFFF;
        public static uint LightMagenta = 0xFF7FFF;
        public static uint LightOrange = 0xFFBF00;
        public static uint LightPurple = 0xBF00FF;
        public static uint LightPink = 0xFFB6C1;
        public static uint LightBrown = 0xD2B48C;
        public static uint Transparent = 0x00000000; // Transparent color
    }

    public static class Math
    {
        public static int Abs(int value)
        {
            return value < 0 ? -value : value;
        }

        public static int Min(int a, int b)
        {
            return a < b ? a : b;
        }

        public static int Max(int a, int b)
        {
            return a > b ? a : b;
        }
    }
    public static unsafe class MemoryOp
    {

        private static ulong HeapBase;
        private static ulong HeapEnd;
        private static ulong FreeListHead;

        public static void InitializeHeap(ulong heapBase, ulong heapSize)
        {
            HeapBase = heapBase;
            HeapEnd = heapBase + heapSize;
            FreeListHead = heapBase;

            // Initialize the free list with a single large block
            *(ulong*)FreeListHead = heapSize; // Block size
            *((ulong*)FreeListHead + 1) = 0; // Next block pointer
        }
        public static unsafe char[] AllocCharArray(int length)
        {
            return null;
        }

        public static IntPtr AllocSafe(uint size)
        {
            void* ptr = Alloc(size);
            if (ptr == null)
            {
                throw null;
            }
            return new IntPtr(ptr);
        }

        public static void* Alloc(uint size)
        {
            if (size == 0)
            {
                return null; // No allocation needed
            }
            size = (uint)((size + 7) & ~7); // Align size to 8 bytes
            ulong prev = 0;
            ulong current = FreeListHead;

            while (current != 0)
            {
                ulong blockSize = *(ulong*)current;
                ulong next = *((ulong*)current + 1);

                if (blockSize >= size + 16) // Enough space for allocation and metadata
                {
                    ulong remaining = blockSize - size - 16;
                    if (remaining >= 16) // Split block
                    {
                        *(ulong*)(current + 16 + size) = remaining;
                        *((ulong*)(current + 16 + size) + 1) = next;
                        *((ulong*)current + 1) = current + 16 + size;
                    }
                    else // Use entire block
                    {
                        size = (uint)(blockSize) - 16;
                        *((ulong*)current + 1) = next;
                    }

                    if (prev == 0)
                    {
                        FreeListHead = *((ulong*)current + 1);
                    }
                    else
                    {
                        *((ulong*)prev + 1) = *((ulong*)current + 1);
                    }

                    *(ulong*)current = size; // Store allocated size
                    return (void*)(current + 16);
                }

                prev = current;
                current = next;
            }

            return null; // Out of memory
        }

        public static void Free(void* ptr)
        {
            ulong block = (ulong)ptr - 16;
            ulong blockSize = *(ulong*)block;

            *(ulong*)block = blockSize + 16;
            *((ulong*)block + 1) = FreeListHead;
            FreeListHead = block;
        }

        public static void FreeSafe(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero)
            {
                return; // Nothing to free
            }

            Free((void*)ptr);
        }

        public static void MemSet(byte* dest, byte value, int count)
        {
            for (int i = 0; i < count; i++)
            {
                dest[i] = value;
            }
        }
        public static void MemSet(uint* dest, uint value, int count)
        {
            for (int i = 0; i < count; i++)
            {
                dest[i] = value;
            }
        }

        public static void MemCopy(uint* dest, uint* src, int count)
        {
            for (int i = 0; i < count; i++)
            {
                dest[i] = src[i];
            }
        }

        public static void MemCopy(byte* dest, byte* src, int count)
        {
            for (int i = 0; i < count; i++)
            {
                dest[i] = src[i];
            }
        }

        public static void MemCopy(char* dest, char* src, int count)
        {
            for (int i = 0; i < count; i++)
            {
                dest[i] = src[i];
            }
        }

        public static bool MemCmp(uint* dest, uint* src, int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (dest[i] != src[i])
                {
                    return false;
                }
            }
            return true;
        }

        public static void MemMove(uint* dest, uint* src, int count)
        {
            if (dest < src)
            {
                for (int i = 0; i < count; i++)
                {
                    dest[i] = src[i];
                }
            }
            else
            {
                for (int i = count - 1; i >= 0; i--)
                {
                    dest[i] = src[i];
                }
            }
        }
    }

    public static class Graphics
    {
        public unsafe class Canvas
        {

            public static uint* Address;
            private static uint* BackBuffer;
            public static uint Width;
            public static uint Height;
            public static uint Pitch;

            public static void Initialize(uint* address, uint width, uint height, uint pitch)
            {
                Address = address;
                Width = width;
                Height = height;
                Pitch = pitch;
            }

            public static void DrawPixel(uint color, int x, int y)
            {
                if (x >= 0 && x < Width && y >= 0 && y < Height)
                {
                    Address[y * (int)(Pitch / 4) + x] = color;
                }
            }

            public static void DrawLine(uint color, int x1, int y1, int x2, int y2)
            {
                int dx = x2 - x1;
                int dy = y2 - y1;
                int absDx = Math.Abs(dx);
                int absDy = Math.Abs(dy);
                int sx = (dx > 0) ? 1 : -1;
                int sy = (dy > 0) ? 1 : -1;
                int err = absDx - absDy;

                while (true)
                {
                    DrawPixel(color, x1, y1);
                    if (x1 == x2 && y1 == y2) break;
                    int err2 = err * 2;
                    if (err2 > -absDy)
                    {
                        err -= absDy;
                        x1 += sx;
                    }
                    if (err2 < absDx)
                    {
                        err += absDx;
                        y1 += sy;
                    }
                }
            }

            public static void DrawRectangle(uint color, int x, int y, int width, int height)
            {
                for (int i = 0; i < width; i++)
                {
                    for (int j = 0; j < height; j++)
                    {
                        DrawPixel(color, x + i, y + j);
                    }
                }
            }

            public static void DrawCircle(uint color, int x, int y, int radius)
            {
                for (int i = -radius; i <= radius; i++)
                {
                    for (int j = -radius; j <= radius; j++)
                    {
                        if (i * i + j * j <= radius * radius)
                        {
                            int px = x + j;
                            int py = y + i;
                            if (px >= 0 && px < Width && py >= 0 && py < Height)
                            {
                                DrawPixel(color, px, py);
                            }
                        }
                    }
                }
            }

            public static void ClearScreen(uint color)
            {
                MemoryOp.MemSet(Address, color, (int)((Pitch / 4) * Height));
            }

            public static void DrawChar(char c, int x, int y, uint color)
            {
                PCScreenFont.PutChar(c, x, y, color, Color.Transparent);
            }

            public static void DrawString(string text, int x, int y, uint color)
            {
                PCScreenFont.PutString(text, x, y, color, Color.Transparent);
            }
            
            public static void DrawString(char* text, int x, int y, uint color)
            {
                PCScreenFont.PutString(text, x, y, color, Color.Transparent);
            }
        }
    }
}

public static class RTC
{
    public enum TimeZone
    {
        UTC,
        GMT,
        EST, // Eastern Standard Time
        CST, // Central Standard Time
        MST, // Mountain Standard Time
        PST, // Pacific Standard Time
        IST, // Indian Standard Time
        CET, // Central European Time
        EET, // Eastern European Time
        JST, // Japan Standard Time
        AEST // Australian Eastern Standard Time
    }

    public enum DaylightSaving
    {
        None,
        Standard,
        Daylight
    }

    public static TimeZone CurrentTimeZone { get; set; } = TimeZone.UTC;
    public static DaylightSaving CurrentDaylightSaving { get; set; } = DaylightSaving.None;

    public static void Initialize()
    {
        // Initialize the RTC by selecting the control register and disabling NMI
        IO.Write8(0x70, 0x0B); // Select register B
        byte control = IO.Read8(0x71);
        control &= 0x7F; // Clear bit 7 to disable NMI
        IO.Write8(0x71, control); // Write back to register B
    }
    private static void WaitForUpdate()
    {
        IO.Write8(0x70, 0x0A); // Select register A
        while ((IO.Read8(0x71) & 0x80) != 0) ; // Wait for update in progress
    }

    private static byte BCD2BIN(byte bcd)
    {
        return (byte)(((bcd >> 4) * 10) + (bcd & 0x0F));
    }

    public static (byte hours, byte minutes, byte seconds) GetAdjustedTime()
    {
        byte hours = GetHours();
        byte minutes = GetMinutes();
        byte seconds = GetSeconds();

        int offset = GetTimeZoneOffset(CurrentTimeZone);

        // Apply offset and wrap around 24-hour time
        int adjustedHours = (hours + offset) % 24;
        if (adjustedHours < 0)
            adjustedHours += 24;

        return ((byte)adjustedHours, minutes, seconds);
    }

    private static int GetTimeZoneOffset(TimeZone timeZone)
    {
        return timeZone switch
        {
            TimeZone.UTC => 0,
            TimeZone.GMT => 0,
            TimeZone.EST => -5,
            TimeZone.CST => -6,
            TimeZone.MST => -7,
            TimeZone.PST => -8,
            TimeZone.IST => 5, // Add 30 minutes later
            TimeZone.CET => 1,
            TimeZone.EET => 2,
            TimeZone.JST => 9,
            TimeZone.AEST => 10,
            _ => 0,
        };
    }

    private static int GetTimeZoneOffsetMinutes()
    {
        return CurrentTimeZone switch
        {
            TimeZone.UTC => 0,
            TimeZone.GMT => 0,
            TimeZone.EST => -300,
            TimeZone.CST => -360,
            TimeZone.MST => -420,
            TimeZone.PST => -480,
            TimeZone.IST => 330,  // India Standard Time = UTC+5:30
            TimeZone.CET => 60,
            TimeZone.EET => 120,
            TimeZone.JST => 540,
            TimeZone.AEST => 600,
            _ => 0,
        };
    }


    

    public static byte GetSeconds()
    {
        WaitForUpdate();
        IO.Write8(0x70, 0x00); // Select seconds register
        byte rawSeconds = IO.Read8(0x71);
        return BCD2BIN(rawSeconds);
    }

    public static byte GetHours()
    {
        WaitForUpdate();
        IO.Write8(0x70, 0x04); // Hours
        byte rawHour = IO.Read8(0x71);
        IO.Write8(0x70, 0x02); // Minutes
        byte rawMinute = IO.Read8(0x71);

        int hour = BCD2BIN((byte)(rawHour & 0x3F));
        int minute = BCD2BIN(rawMinute);

        int totalMinutes = hour * 60 + minute;
        totalMinutes += GetTimeZoneOffsetMinutes();

        if (CurrentDaylightSaving == DaylightSaving.Daylight)
            totalMinutes += 60;

        if (totalMinutes < 0)
            totalMinutes += 1440;
        totalMinutes %= 1440;

        return (byte)(totalMinutes / 60);
    }

    public static byte GetMinutes()
    {
        WaitForUpdate();
        IO.Write8(0x70, 0x04); // Hours
        byte rawHour = IO.Read8(0x71);
        IO.Write8(0x70, 0x02); // Minutes
        byte rawMinute = IO.Read8(0x71);

        int hour = BCD2BIN((byte)(rawHour & 0x3F));
        int minute = BCD2BIN(rawMinute);

        int totalMinutes = hour * 60 + minute;
        totalMinutes += GetTimeZoneOffsetMinutes();

        if (CurrentDaylightSaving == DaylightSaving.Daylight)
            totalMinutes += 60;

        if (totalMinutes < 0)
            totalMinutes += 1440;
        totalMinutes %= 1440;

        return (byte)(totalMinutes % 60);
    }


    public static byte GetDayOfWeek()
    {
        WaitForUpdate();
        IO.Write8(0x70, 0x06); // Select day of week register
        byte rawDayOfWeek = IO.Read8(0x71);
        return (byte)(rawDayOfWeek & 0x07); // Mask to 0-6 (Sunday-Saturday)
    }

    public static byte GetDayOfMonth()
    {
        WaitForUpdate();
        IO.Write8(0x70, 0x07); // Select day of month register
        byte rawDayOfMonth = IO.Read8(0x71);
        return BCD2BIN(rawDayOfMonth);
    }

    public static byte GetMonth()
    {
        WaitForUpdate();
        IO.Write8(0x70, 0x08); // Select month register
        byte rawMonth = IO.Read8(0x71);
        return BCD2BIN((byte)(rawMonth & 0x1F)); // Mask to 1-12
    }

    public static ushort GetYear()
    {
        WaitForUpdate();
        IO.Write8(0x70, 0x09); // Select year register
        byte rawYear = IO.Read8(0x71);
        return (ushort)(BCD2BIN(rawYear) + 2000); // Convert to full year (assuming 2000s)
    }

    public static string GetMonthName()
    {
        byte month = GetMonth();
        switch (month)
        {
            case 1: return "January";
            case 2: return "February";
            case 3: return "March";
            case 4: return "April";
            case 5: return "May";
            case 6: return "June";
            case 7: return "July";
            case 8: return "August";
            case 9: return "September";
            case 10: return "October";
            case 11: return "November";
            case 12: return "December";
            default: return "Unknown";
        }
    }

    public static string GetDayOfWeekName()
    {
        byte dayOfWeek = GetDayOfWeek();
        switch (dayOfWeek)
        {
            case 0: return "Sunday";
            case 1: return "Monday";
            case 2: return "Tuesday";
            case 3: return "Wednesday";
            case 4: return "Thursday";
            case 5: return "Friday";
            case 6: return "Saturday";
            default: return "Unknown";
        }
    }

    public static unsafe char* GetTime()
    {
        byte hours = GetHours();
        byte minutes = GetMinutes();
        byte seconds = GetSeconds();

        char* timeStr = (char*)MemoryOp.Alloc(9 * sizeof(char)); // "HH:MM:SS\0"

        char* hourString = StrConversion.ToString(hours);
        char* minuteString = StrConversion.ToString(minutes);
        char* secondString = StrConversion.ToString(seconds);

        int hourLen = (int)StringOp.StrLen(hourString);
        int minuteLen = (int)StringOp.StrLen(minuteString);
        int secondLen = (int)StringOp.StrLen(secondString);

        // Optional: pad with leading zeros
        if (hourLen == 1)
        {
            timeStr[0] = '0';
            timeStr[1] = hourString[0];
        }
        else
        {
            MemoryOp.MemCopy(timeStr, hourString, hourLen);
        }

        timeStr[2] = ':';

        if (minuteLen == 1)
        {
            timeStr[3] = '0';
            timeStr[4] = minuteString[0];
        }
        else
        {
            MemoryOp.MemCopy(timeStr + 3, minuteString, minuteLen);
        }

        timeStr[5] = ':';

        if (secondLen == 1)
        {
            timeStr[6] = '0';
            timeStr[7] = secondString[0];
        }
        else
        {
            MemoryOp.MemCopy(timeStr + 6, secondString, secondLen);
        }

        timeStr[8] = '\0';

        MemoryOp.Free(hourString);
        MemoryOp.Free(minuteString);
        MemoryOp.Free(secondString);

        return timeStr;
    }

    public static unsafe char* GetDate()
    {
        byte day = GetDayOfMonth();
        byte month = GetMonth();
        ushort year = GetYear();

        char* dateStr = (char*)MemoryOp.Alloc(11 * sizeof(char)); // "DD/MM/YYYY\0"

        char* daystring = StrConversion.ToString(day);
        char* monthstring = StrConversion.ToString(month);
        char* yearstring = StrConversion.ToString(year);

        int dayLen = (int)StringOp.StrLen(daystring);
        int monthLen = (int)StringOp.StrLen(monthstring);
        int yearLen = (int)StringOp.StrLen(yearstring);

        // Optional: pad with leading zeros
        if (dayLen == 1)
        {
            dateStr[0] = '0';
            dateStr[1] = daystring[0];
        }
        else
        {
            MemoryOp.MemCopy(dateStr, daystring, dayLen);
        }

        dateStr[2] = '/';

        if (monthLen == 1)
        {
            dateStr[3] = '0';
            dateStr[4] = monthstring[0];
        }
        else
        {
            MemoryOp.MemCopy(dateStr + 3, monthstring, monthLen);
        }

        dateStr[5] = '/';

        // Pad year with zeros if needed
        for (int i = 0; i < 4 - yearLen; i++)
        {
            dateStr[6 + i] = '0';
        }
        MemoryOp.MemCopy(dateStr + 6 + (4 - yearLen), yearstring, yearLen);

        dateStr[10] = '\0';

        MemoryOp.Free(daystring);
        MemoryOp.Free(monthstring);
        MemoryOp.Free(yearstring);

        return dateStr;
    }

    public static unsafe char* GetDateTime()
    {
        char* date = GetDate();
        char* time = GetTime();

        int dateLen = (int)StringOp.StrLen(date);
        int timeLen = (int)StringOp.StrLen(time);

        char* dateTimeStr = (char*)MemoryOp.Alloc((uint)((dateLen + timeLen + 2) * sizeof(char))); // "DD/MM/YYYY HH:MM:SS\0"

        MemoryOp.MemCopy(dateTimeStr, date, dateLen);
        dateTimeStr[dateLen] = ' ';
        MemoryOp.MemCopy(dateTimeStr + dateLen + 1, time, timeLen);
        dateTimeStr[dateLen + 1 + timeLen] = '\0';

        MemoryOp.Free(date);
        MemoryOp.Free(time);

        return dateTimeStr;
    }

    public static void SetTimeZone(TimeZone timeZone)
    {
        CurrentTimeZone = timeZone;
    }

    public static void SetDaylightSaving(DaylightSaving daylightSaving)
    {
        CurrentDaylightSaving = daylightSaving;
    }

}