#include "os.h"

extern void Canvas_ClearScreen(uint color);
extern void Canvas_DrawChar(System_Char c, System_Int32 x, System_Int32 y, System_UInt32 color);
extern void Canvas_DrawString_Managed(System_String str, System_Int32 x, System_Int32 y, System_UInt32 color);
extern void Canvas_DrawString_Unmanaged(char str[], System_Int32 x, System_Int32 y, System_UInt32 color);

NO_NAME_MANGLE void test_gcc_function(void) {
    Canvas_ClearScreen(0x000000);
    char test[] = {
        'H', 'e', 'l', 'l', 'o', ' ', 'W', 'o', 'r', 'l', 'd', '\0'
    };
    Canvas_DrawString_Unmanaged(test, 10, 10, 0xFFFFFF);
}

NO_NAME_MANGLE char GetCharAt(System_String str, System_Int32 index) {
    if (index < 0 || index >= str._stringLength) {
        return '\0'; // Return null character for out-of-bounds access
    }
    return ((char*)&str._firstChar)[index * sizeof(System_Char)];
}