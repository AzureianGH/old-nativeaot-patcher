#ifndef _OS_H
#define _OS_H
#define NO_NAME_MANGLE __attribute__((used))

#include "stdint.h"
// Signed
typedef int8_t    System_Int8;
typedef int16_t   System_Int16;
typedef int32_t   System_Int32;
typedef int64_t   System_Int64;

// Unsigned
typedef uint8_t   System_UInt8;
typedef uint16_t  System_UInt16;
typedef uint32_t  System_UInt32;
typedef uint64_t  System_UInt64;

typedef uint16_t System_Char;

typedef intptr_t System_IntPtr;

typedef struct {
    void* _methodTable;          // 0x00: .NET object header/type pointer
    System_Int32 _stringLength;  // 0x08: length of the string in characters
    System_Char _firstChar;      // 0x0C: first character (2 bytes)
    // Remaining (length - 1) characters follow in memory
} System_String;

typedef signed char        sbyte;
typedef unsigned char      byte;
typedef unsigned short     ushort;
typedef unsigned int       uint;
typedef unsigned long long ulong;

void Canvas_ClearScreen(uint color);

NO_NAME_MANGLE char GetCharAt(System_String str, System_Int32 index);

#endif // _OS_H