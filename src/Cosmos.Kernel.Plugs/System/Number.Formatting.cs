using Cosmos.Build.API.Attributes;
using Cosmos.Kernel.System.Graphics;

namespace Cosmos.Kernel.Plugs.System
{
    [Plug("System.Number")]
    public class NumPlug
    {
        [PlugMember]
        public static string UInt32ToDecStr(uint value)
        {
            // Convert to base 10 string representation

            if (value == 0) return "0";
            char[] buffer = new char[10]; // Max length for uint32 in decimal is 10 digits
            int pos = 10;
            while (value > 0)
            {
                buffer[--pos] = (char)('0' + (value % 10));
                value /= 10;
            }
            return new string(buffer, pos, 10 - pos);
        }
    }
}