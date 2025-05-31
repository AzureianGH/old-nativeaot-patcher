#ifndef _STDDEF_H
#define _STDDEF_H

#include "stdint.h"

// Define the NULL pointer
#define NULL ((void*)0)
// Define the offsetof macro for calculating offsets in structures
#define offsetof(type, member) ((size_t)&(((type*)0)->member))
// Define the max alignment for types
#define alignof(type) __alignof__(type)
// Define the alignas macro for specifying alignment requirements
#define alignas(type) __attribute__((aligned(alignof(type))))
// Define the restrict keyword for pointer aliasing
#define restrict __restrict__
// Define the static_assert macro for compile-time assertions
#define static_assert(expr, msg) _Static_assert(expr, msg)
// Define the _Alignas macro for specifying alignment requirements
#define _Alignas(type) __attribute__((aligned(alignof(type))))
// Define the _Alignof macro for calculating alignment requirements
#define _Alignof(type) alignof(type)
// Define the _Noreturn macro for functions that do not return
#define _Noreturn __attribute__((noreturn))

#endif // _STDDEF_H