using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using EarlyBird.Conversion;

namespace EarlyBird.Conversion
{

    public static unsafe class StrConversion
    {

    // public static string FromCharPointer(char* input)
    // {
    //     if (input == null)
    //         return string.Empty;

    //     // 1. Calculate length
    //     int len = 0;
    //     char* tmp = input;
    //     while (*tmp++ != '\0') len++;
        
    //     // 2. Create a managed string
    //     string result = new string('\0', len);
        
    //     // 3. Copy characters from native memory to managed string
    //     fixed (char* pDest = result)
    //     {
    //         for (int i = 0; i < len; i++)
    //         {
    //             pDest[i] = input[i];
    //         }
    //     }
        
    //     return result;
    // }

        public static char* ToString(sbyte b)
        {
            char* str = (char*)MemoryOp.Alloc(5); // max "-128" + '\0' = 5 chars

            int len = 0;
            if (b < 0)
            {
                str[len++] = '-';
                b = (sbyte)-b; // make it positive for conversion
            }

            if (b >= 100)
            {
                str[len++] = (char)('0' + b / 100);
                b %= 100;
                str[len++] = (char)('0' + b / 10);
                b %= 10;
                str[len++] = (char)('0' + b);
            }
            else if (b >= 10)
            {
                str[len++] = (char)('0' + b / 10);
                b %= 10;
                str[len++] = (char)('0' + b);
            }
            else
            {
                str[len++] = (char)('0' + b);
            }

            str[len] = '\0'; // null terminator
            return str;
        }

        public static char* ToString(byte b)
        {
            char* str = (char*)MemoryOp.Alloc(4); // max "255" + '\0' = 4 chars

            int len = 0;
            if (b >= 100)
            {
                str[len++] = (char)('0' + b / 100);
                b %= 100;
                str[len++] = (char)('0' + b / 10);
                b %= 10;
                str[len++] = (char)('0' + b);
            }
            else if (b >= 10)
            {
                str[len++] = (char)('0' + b / 10);
                b %= 10;
                str[len++] = (char)('0' + b);
            }
            else
            {
                str[len++] = (char)('0' + b);
            }

            str[len] = '\0'; // null terminator
            return str;
        }

        public static char* ToString(ushort b)
        {
            char* str = (char*)MemoryOp.Alloc(6); // max "65535" + '\0' = 6 chars
            int len = 0;
            if (b == 0)
            {
                str[len++] = '0';
            }
            else
            {
                ushort temp = b;
                do
                {
                    str[len++] = (char)('0' + (temp % 10));
                    temp /= 10;
                } while (temp > 0);
            }
            // Reverse the string
            for (int i = 0; i < len / 2; i++)
            {
                (str[i], str[len - 1 - i]) = (str[len - 1 - i], str[i]);
            }
            str[len] = '\0'; // null terminator
            return str;
        }
    }

}

namespace EarlyBird.String
{
    public static unsafe class StringOp
    {

        public static uint StrLen(char* str)
        {
            uint len = 0;
            while (str[len] != '\0')
            {
                len++;
            }
            return len;
        }
        public static char* StrCat(string a, string b)
        {
            int lenA = a.Length;
            int lenB = b.Length;
            char* result = (char*)MemoryOp.Alloc((uint)(lenA + lenB + 1)); // +1 for null terminator

            fixed (char* ptrA = a)
            {
                for (int i = 0; i < lenA; i++)
                {
                    result[i] = ptrA[i];
                }
            }

            fixed (char* ptrB = b)
            {
                for (int i = 0; i < lenB; i++)
                {
                    result[lenA + i] = ptrB[i];
                }
            }

            result[lenA + lenB] = '\0'; // null terminator
            return result;
        }

        public static char* StrCat(char* a, char* b)
        {
            int lenA = 0;
            while (a[lenA] != '\0') lenA++;

            int lenB = 0;
            while (b[lenB] != '\0') lenB++;

            char* result = (char*)MemoryOp.Alloc((uint)(lenA + lenB + 1)); // +1 for null terminator

            for (int i = 0; i < lenA; i++)
            {
                result[i] = a[i];
            }

            for (int i = 0; i < lenB; i++)
            {
                result[lenA + i] = b[i];
            }

            result[lenA + lenB] = '\0'; // null terminator
            return result;
        }

        [StructLayout(LayoutKind.Sequential)]
        private class LikeString
        {
            private const int POINTER_SIZE = 8;

            internal const int FIRST_CHAR_OFFSET = 12;

            public int _stringLength;

            public char _firstChar;

            public int Length => _stringLength;

            public LikeString(int length)
            {
                _stringLength = length;
                _firstChar = '\0'; // Initialize first character
            }

            public LikeString(char firstChar, int length)
            {
                _stringLength = length;
                _firstChar = firstChar; // Initialize first character
            }
            public char this[int index]
            {
                get
                {
                    if (index < 0 || index >= _stringLength)
                    {
                        throw new IndexOutOfRangeException("Index out of range");
                    }
                    return Unsafe.Add(ref _firstChar, index);
                }
                set
                {
                    if (index < 0 || index >= _stringLength)
                    {
                        throw new IndexOutOfRangeException("Index out of range");
                    }
                    Unsafe.Add(ref _firstChar, index) = value;
                }
            }
        }

        

    }

    public static unsafe class StringExtensions
    {

        public static char[] ToCharArray(this string str)
        {
            char[] result = new char[str.Length];
            fixed (char* strPtr = str)
            {
                for (int i = 0; i < str.Length; i++)
                {
                    result[i] = strPtr[i];
                }
            }
            return result;
        }

        public unsafe static Span<char> ToCharSpan(this string str)
        {
            char* ptr = (char*)MemoryOp.Alloc((uint)(str.Length * sizeof(char)));

            fixed (char* strPtr = str)
            {
                for (int i = 0; i < str.Length; i++)
                {
                    ptr[i] = strPtr[i];
                }
            }

            return new Span<char>(ptr, str.Length);
        }


        public static char GetCharAt(this string str, int index)
        {
            if (index < 0 || index >= str.Length)
            {
                return '\0'; // or throw an exception
            }

            fixed (char* ptr = str)
            {
                // since utf-16, it looks like this 'A', '\0', 'b', '\0', 'c', '\0'..., so we have to skip every second byte
                return ptr[index]; // each char is 2 bytes in UTF-16
            }
        }


        public static char* ToString(this sbyte b)
        {
            return StrConversion.ToString(b);
        }
        public static char* ToString(this byte b)
        {
            return StrConversion.ToString(b);
        }
        public static char* ToString(this ushort b)
        {
            return StrConversion.ToString(b);
        }
        public static char* ToRaw(this string str)
        {
            int len = str.Length;
            char* result = (char*)MemoryOp.Alloc((uint)(len + 1)); // +1 for null terminator

            fixed (char* ptr = str)
            {
                for (int i = 0; i < len; i++)
                {
                    result[i] = ptr[i];
                }
            }

            result[len] = '\0'; // null terminator
            return result;
        }
    }

}

namespace System
{
    public partial class String
    {
        //ConCat
        
    }
}