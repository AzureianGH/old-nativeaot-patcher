using System.Numerics;
using Cosmos.Build.API.Attributes;
using Cosmos.Kernel.System.Graphics;

namespace Cosmos.Kernel.Plugs.System
{
    [Plug("System.Number")]
    public class NumPlug
    {
        [PlugMember]
        public static string UInt64ToDecStr(ulong value)
        {
            // Convert to base 10 string representation

            if (value == 0)
                return "0";
            char[] buffer = new char[20]; // Max length for uint64 in decimal is 20 digits
            int pos = 20;
            while (value > 0)
            {
                buffer[--pos] = (char)('0' + (value % 10));
                value /= 10;
            }
            return new string(buffer, pos, 20 - pos);
        }

        [PlugMember]
        public static string UInt64ToHexStr(ulong value)
        {
            // Convert to base 16 string representation

            if (value == 0)
                return "0";
            char[] buffer = new char[16]; // Max length for uint64 in hex is 16 digits
            int pos = 16;
            while (value > 0)
            {
                int digit = (int)(value & 0xF);
                buffer[--pos] = (char)(digit < 10 ? '0' + digit : 'A' + (digit - 10));
                value >>= 4;
            }
            return new string(buffer, pos, 16 - pos);
        }

        [PlugMember]
        public static string Int64ToDecStr(long value)
        {
            // Handle negative numbers
            if (value < 0)
            {
                return "-" + UInt64ToDecStr((ulong)(-value));
            }
            return UInt64ToDecStr((ulong)value);
        }

        [PlugMember]
        public static string Int64ToHexStr(long value)
        {
            // Handle negative numbers by converting to ulong
            return UInt64ToHexStr((ulong)value);
        }

        [PlugMember]
        public static string UInt32ToDecStr(uint value)
        {
            // Convert to base 10 string representation

            if (value == 0)
                return "0";
            char[] buffer = new char[10]; // Max length for uint32 in decimal is 10 digits
            int pos = 10;
            while (value > 0)
            {
                buffer[--pos] = (char)('0' + (value % 10));
                value /= 10;
            }
            return new string(buffer, pos, 10 - pos);
        }

        [PlugMember]
        public static string UInt32ToHexStr(uint value)
        {
            // Convert to base 16 string representation

            if (value == 0)
                return "0";
            char[] buffer = new char[8]; // Max length for uint32 in hex is 8 digits
            int pos = 8;
            while (value > 0)
            {
                int digit = (int)(value & 0xF);
                buffer[--pos] = (char)(digit < 10 ? '0' + digit : 'A' + (digit - 10));
                value >>= 4;
            }
            return new string(buffer, pos, 8 - pos);
        }

        [PlugMember]
        public static string Int32ToDecStr(int value)
        {
            // Handle negative numbers
            if (value < 0)
            {
                return "-" + UInt32ToDecStr((uint)(-value));
            }
            return UInt32ToDecStr((uint)value);
        }

        [PlugMember]
        public static string Int32ToHexStr(int value)
        {
            // Handle negative numbers by converting to uint
            return UInt32ToHexStr((uint)value);
        }
    }
}
