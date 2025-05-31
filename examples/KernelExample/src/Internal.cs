using System;
using System.EarlyBird.Internal.Rhp;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using EarlyBird;
using EarlyBird.Internal;
using Internal.Runtime;
using static EarlyBird.Graphics;


namespace System
{
    namespace Runtime.InteropServices
    {
        public static class Marshal
        {
            public static IntPtr AllocHGlobal(uint cb)
            {
                if (cb < 0)
                    throw new ArgumentOutOfRangeException("Size cannot be negative.");
                return MemoryOp.AllocSafe(cb);
            }

            public static void FreeHGlobal(IntPtr ptr)
            {
                if (ptr == IntPtr.Zero)
                    throw new NullReferenceException("Pointer cannot be null.");

                MemoryOp.FreeSafe(ptr);
            }
        }
    }
    namespace EarlyBird.Internal.Rhp
    {
        class RhpExports
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            [RuntimeExport("ThrowExProper")]
            public static unsafe void ThrowExProper(Exception pException)
            {
                
                Canvas.DrawString(pException.Message, 0, 0, Color.Red);
                Serial.WriteString("An exception has occurred. Halting execution.\r\n");
                Serial.WriteString("Exception: ");
                Serial.WriteString(pException.Message);
                Canvas.DrawString("HALTED", 0, (int)Canvas.Height - 28, Color.Red);
                while (true) ;
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            [RuntimeExport("ThrowExImproper")]
            public static unsafe void ThrowExImproper(Exception pException)
            {
                Canvas.DrawString("An unknown exception has occurred.", 0, 0, Color.Red);
                Serial.WriteString("An unknown exception has occurred. Halting execution.\r\n");
                Canvas.DrawString("HALTED", 0, (int)Canvas.Height - 28, Color.Red);
                while (true) ;
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            [RuntimeExport("RhpNewFast")]
            public static unsafe IntPtr RhpNewFast(MethodTable* pEEType)
            {
                // Allocate memory for the object based on its EEType size
                IntPtr objectPtr = (IntPtr)MemoryOp.Alloc(pEEType->_uBaseSize);

                // Set the MethodTable (vtable pointer) at the beginning of the object
                // Assuming object layout: [MethodTable*][object fields...]
                *(MethodTable**)objectPtr = pEEType;

                return objectPtr;
            }
            [MethodImpl(MethodImplOptions.NoInlining)]
            [RuntimeExport("RhpNewArray")]
            public static unsafe Array* NewArray(MethodTable* pEEType, int num)
            {
                if (num < 0)
                    throw new ArgumentOutOfRangeException("Array size cannot be negative.");

                // Calculate the size of the array
                int size = (int)(pEEType->_uBaseSize * num);

                // Allocate memory for the array
                Array* arrayPtr = (Array*)MemoryOp.Alloc((uint)size);

                // Set the MethodTable (vtable pointer) at the beginning of the array
                *(MethodTable**)arrayPtr = pEEType;

                // Initialize the length of the array using _numComponents
                arrayPtr->_numComponents = num;

                return arrayPtr;
            }
            [MethodImpl(MethodImplOptions.NoInlining)]
            [RuntimeExport("RhpAssignRef")]
            public static unsafe void AssignRef(ref object destination, object source)
            {

                destination = source;
            }
            [MethodImpl(MethodImplOptions.NoInlining)]
            [RuntimeExport("RhUnbox2")]
            public static unsafe ref byte RhUnbox2(MethodTable* pUnboxToEEType, object obj)
            {
                return ref obj.GetRawData();
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            [RuntimeExport("RhNewString")]
            public static unsafe string* RhNewString_Lower(MethodTable* pArrayEEType, int numElements)
            {
                return (string*)NewArray(pArrayEEType, numElements);
            }

            [MethodImpl(MethodImplOptions.InternalCall)]
            [RuntimeImport("*", "RhNewString")]
            public static unsafe extern string RhNewString(MethodTable* pArrayEEType, int numElements);
            
        }
    }

    public static class AZRuntimeHelpers
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        [RuntimeExport("_FastAllocateString")]
        public static unsafe string FastAllocateString(int length)
        {
            return RhpExports.RhNewString(MethodTable.Of<string>(), length);
        }

    }

    public readonly ref struct Span<T>
    {
        private readonly T[] _array;
        private readonly int _start;
        private readonly int _length;

        public Span(T[] array)
        {
            _array = array ?? throw null;
            _start = 0;
            _length = array.Length;
        }

        public Span(T[] array, int start, int length)
        {
            if (array == null) throw null;
            if (start < 0 || length < 0 || start + length > array.Length)
                throw null;

            _array = array;
            _start = start;
            _length = length;
        }

        public unsafe Span(char* ptr, int length)
        {
            if (ptr == null) throw null;
            if (length < 0) throw null;

            _array = new T[length];
            _start = 0;
            _length = length;

            for (int i = 0; i < length; i++)
            {
                _array[i] = (T)(object)ptr[i]; // Unsafe cast, assuming T is char
            }
        }

        public int Length => _length;

        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= _length)
                    throw new IndexOutOfRangeException();
                return _array[_start + index];
            }
            set
            {
                if (index < 0 || index >= _length)
                    throw new IndexOutOfRangeException();
                _array[_start + index] = value;
            }
        }
    }

    namespace Collections
    {
        public interface IEnumerable<T>
        {
            IEnumerator<T> GetEnumerator();
        }

        public interface IEnumerator<T>
        {
            T Current { get; }
            bool MoveNext();
            void Reset();
        }

        public interface ICollection<T> : IEnumerable<T>
        {
            int Count { get; }
            bool IsReadOnly { get; }
            void Add(T item);
            void Clear();
            bool Contains(T item);
            void CopyTo(T[] array, int arrayIndex);
            bool Remove(T item);
        }
    }

    public class IndexOutOfRangeException : Exception
    {
        public IndexOutOfRangeException() : base("Index was outside the bounds of the array.")
        {
        }

        public IndexOutOfRangeException(string message) : base(message)
        {
        }

    }

    public class NullReferenceException : Exception
    {
        public NullReferenceException() : base("Object reference not set to an instance of an object.")
        {
        }

        public NullReferenceException(string message) : base(message)
        {
        }

    }

    public class ArgumentOutOfRangeException : Exception
    {
        public ArgumentOutOfRangeException() : base("Specified argument was out of the range of valid values.")
        {
        }

        public ArgumentOutOfRangeException(string message) : base(message)
        {
        }

    }

    public struct ValueTuple<T1>
    {
        public T1 Item1;

        public ValueTuple(T1 item1)
        {
            Item1 = item1;
        }
    }

    public struct ValueTuple<T1, T2>
    {
        public T1 Item1;
        public T2 Item2;

        public ValueTuple(T1 item1, T2 item2)
        {
            Item1 = item1;
            Item2 = item2;
        }
    }

    public struct ValueTuple<T1, T2, T3>
    {
        public T1 Item1;
        public T2 Item2;
        public T3 Item3;

        public ValueTuple(T1 item1, T2 item2, T3 item3)
        {
            Item1 = item1;
            Item2 = item2;
            Item3 = item3;
        }
    }

    public struct ValueTuple<T1, T2, T3, T4>
    {
        public T1 Item1;
        public T2 Item2;
        public T3 Item3;
        public T4 Item4;

        public ValueTuple(T1 item1, T2 item2, T3 item3, T4 item4)
        {
            Item1 = item1;
            Item2 = item2;
            Item3 = item3;
            Item4 = item4;
        }
    }

    namespace Runtime
    {

        namespace CompilerServices
        {
            //ExtensionAttribute
            [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field, Inherited = false)]
            internal sealed class ExtensionAttribute : Attribute
            {
                public ExtensionAttribute()
                {
                }
            }
            // TupleElementNamesAttribute
            [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
            internal sealed class TupleElementNamesAttribute : Attribute
            {
                public string[] TransformNames { get; }

                public TupleElementNamesAttribute(string[] transformNames)
                {
                    TransformNames = transformNames ?? throw null;
                }
            }
        }
        internal sealed class RuntimeExportAttribute(string entry) : Attribute
        {
        }

        internal sealed class RuntimeImportAttribute : Attribute
        {
            public string DllName { get; }
            public string EntryPoint { get; }

            public RuntimeImportAttribute(string entry)
            {
                EntryPoint = entry;
            }

            public RuntimeImportAttribute(string dllName, string entry)
            {
                EntryPoint = entry;
                DllName = dllName;
            }
        }
    }
}

namespace Internal.Runtime.CompilerHelpers
{
    // A class that the compiler looks for that has helpers to initialize the
    // process. The compiler can gracefully handle the helpers not being present,
    // but the class itself being absent is unhandled. Let's add an empty class.
    class StartupCodeHelpers
    {
        // A couple symbols the generated code will need we park them in this class
        // for no particular reason. These aid in transitioning to/from managed code.
        // Since we don't have a GC, the transition is a no-op.
        [RuntimeExport("RhpReversePInvoke")]
        static void RhpReversePInvoke(IntPtr frame) { }
        [RuntimeExport("RhpReversePInvokeReturn")]
        static void RhpReversePInvokeReturn(IntPtr frame) { }
    }
}

namespace EarlyBird.Internal
{

    public static class Native
    {
        public static class IO
        {
            [MethodImpl(MethodImplOptions.InternalCall)]
            [RuntimeImport("*", "_native_io_write_byte")]
            public static extern void Write8(ushort Port, byte Value);

            [MethodImpl(MethodImplOptions.InternalCall)]
            [RuntimeImport("*", "_native_io_write_word")]
            public static extern void Write16(ushort Port, ushort Value);

            [MethodImpl(MethodImplOptions.InternalCall)]
            [RuntimeImport("*", "_native_io_write_dword")]
            public static extern void Write32(ushort Port, uint Value);

            [MethodImpl(MethodImplOptions.InternalCall)]
            [RuntimeImport("*", "_native_io_read_byte")]
            public static extern byte Read8(ushort Port);

            [MethodImpl(MethodImplOptions.InternalCall)]
            [RuntimeImport("*", "_native_io_read_word")]
            public static extern ushort Read16(ushort Port);

            [MethodImpl(MethodImplOptions.InternalCall)]
            [RuntimeImport("*", "_native_io_read_dword")]
            public static extern uint Read32(ushort Port);
        }
    }

    public static class Serial
    {
        private static readonly ushort COM1 = 0x3F8;

        private static void WaitForTransmitBufferEmpty()
        {
            while ((Native.IO.Read8((ushort)(COM1 + 5)) & 0x20) == 0) ;
        }

        public static void ComWrite(byte value)
        {
            // Wait for the transmit buffer to be empty
            WaitForTransmitBufferEmpty();
            // Write the byte to the COM port
            Native.IO.Write8(COM1, value);
        }



        public static void ComInit()
        {
            Native.IO.Write8((ushort)(COM1 + 1), 0x00);
            Native.IO.Write8((ushort)(COM1 + 3), 0x80);
            Native.IO.Write8(COM1, 0x01);
            Native.IO.Write8((ushort)(COM1 + 1), 0x00);
            Native.IO.Write8((ushort)(COM1 + 3), 0x03);
            Native.IO.Write8((ushort)(COM1 + 2), 0xC7);
        }

        public static unsafe void WriteString(string str)
        {
            fixed (char* ptr = str)
            {
                for (int i = 0; i < str.Length; i++)
                {
                    ComWrite((byte)ptr[i]);
                }
            }
        }
    }
}
