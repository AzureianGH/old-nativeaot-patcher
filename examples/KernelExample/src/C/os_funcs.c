#define NO_NAME_MANGLE __attribute__((used))

#include "stdint.h"

NO_NAME_MANGLE uint16_t test_gcc_function() {
    // Return the magic value expected in the test
    return 12345;
}
