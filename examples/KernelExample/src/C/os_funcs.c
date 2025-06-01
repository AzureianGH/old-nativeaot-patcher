#include "os.h"

extern void Canvas_ClearScreen(uint color);
extern void Canvas_DrawChar(System_Char c, System_Int32 x, System_Int32 y, System_UInt32 color);
extern void Canvas_DrawString_Managed(System_String* str, System_Int32 x, System_Int32 y, System_UInt32 color);
extern void Canvas_DrawString_Unmanaged(char str[], System_Int32 x, System_Int32 y, System_UInt32 color);

NO_NAME_MANGLE char GetCharAt(System_String str, System_Int32 index) {
    if (index < 0 || index >= str._stringLength) {
        return '\0'; // Return null character for out-of-bounds access
    }
    return ((char*)&str._firstChar)[index * sizeof(System_Char)];
}

NO_NAME_MANGLE void ChangeCharAt(System_String* str, System_Int32 index, System_Char c) {
    if (index < 0 || index >= str->_stringLength) {
        return; // Out-of-bounds access, do nothing
    }
    ((System_Char*)&str->_firstChar)[index] = c;
}

NO_NAME_MANGLE void test_gcc_function(System_String* str) {
    Canvas_ClearScreen(0x000000);
    Canvas_DrawString_Managed(str, 0, 0, 0xFFFFFF);
    ChangeCharAt(str, 7, 'E');
    ChangeCharAt(str, 8, 'a');
    ChangeCharAt(str, 9, 'r');
    ChangeCharAt(str, 10, 't');
    ChangeCharAt(str, 11, 'h');
    Canvas_DrawString_Managed(str, 0, 28, 0xFFFFFF);
}

