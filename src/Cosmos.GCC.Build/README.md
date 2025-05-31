# GCC Integration for NativeAOT Patcher

This extension enables the integration of C code into your NativeAOT kernel project. It compiles C source files into object files (.o) that are linked directly into your kernel ELF executable.

## Usage

To use this functionality, add the `GCCProject` items to your project file:

```xml
<ItemGroup>
  <GCCProject Include="./src/C/DirectoryName/" />
</ItemGroup>
```

Each directory specified with `GCCProject` will have all C files within it compiled separately to object files. These object files are then automatically linked into the final kernel ELF binary.

## Requirements

- **x86_64-elf-gcc** cross-compiler is **required** and assumed to be installed and available in the PATH
- The build system will always use x86_64-elf-gcc as the compiler command

### Installing x86_64-elf-gcc

The cross-compiler must be installed before building projects with C code. x86_64-elf-gcc is a cross-compiler that generates ELF binaries specifically for x86_64 systems, which is required for kernel development.

#### Windows
```powershell
# Option 1: Using MSYS2 (recommended)
# 1. Install MSYS2 from https://www.msys2.org/
# 2. Open MSYS2 MINGW64 shell and run:
pacman -S mingw-w64-x86_64-gcc mingw-w64-x86_64-binutils
pacman -S mingw-w64-x86_64-gcc-libs mingw-w64-x86_64-gdb
pacman -S --needed base-devel mingw-w64-x86_64-toolchain

# Option 2: Using pre-built cross-compiler
# Download from https://github.com/mstorsjo/llvm-mingw/releases
# Extract to C:\tools\cross

# Option 3: Run the provided script
./install-gcc-cross.ps1
```

#### Linux/macOS
```bash
# Ubuntu/Debian
sudo apt-get install gcc-x86-64-linux-gnu binutils-x86-64-linux-gnu

# Arch Linux
sudo pacman -S x86_64-elf-gcc x86_64-elf-binutils

# macOS (with Homebrew)
brew install x86_64-elf-gcc

# Manual installation (all platforms)
git clone https://github.com/lordmilko/i686-elf-tools
cd i686-elf-tools
./build-x86_64-elf.sh
```

## Configuration

You can configure the GCC build process with the following properties:

```xml
<PropertyGroup>
  <!-- Optional: Path to GCC/Clang executable -->
  <GCCPath>path/to/x86_64-elf-gcc</GCCPath>
  
  <!-- Optional: Additional compiler flags -->
  <GCCCompilerFlags>-O2 -fno-stack-protector -nostdinc</GCCCompilerFlags>
</PropertyGroup>
```

## Example

```xml
<ItemGroup>
  <GCCProject Include="./src/C/Drivers/" />
  <GCCProject Include="./src/C/Lib/" />
</ItemGroup>
```

This will compile all C files in the specified directories into object files, which will be linked into the kernel.

## Build Order

The C files are compiled after the assembly (.asm) files but before the final linking step. This allows for proper integration with the rest of the build process.

## Interoperability with C#

To call C functions from C#, you'll need to define the proper runtime imports:

```csharp
using System.Runtime.CompilerServices;

public class MyKernel
{
    [MethodImpl(MethodImplOptions.InternalCall)]
    [RuntimeImport("*", "my_c_function")]
    public static extern int MyCFunction(int arg1, int arg2);
}
```

## Function Exports in C

In your C code, make sure to properly export functions so they can be called from C#:

```c
// Use __attribute__((used)) to prevent the compiler from optimizing away the function
__attribute__((used)) int my_c_function(int arg1, int arg2) {
    return arg1 + arg2;
}
```

## Architecture-Specific Code

For architecture-specific code, you can create subdirectories with the architecture name:

```xml
<ItemGroup>
  <GCCProject Include="./src/C/Drivers/" />
</ItemGroup>
```

If your runtime identifier is `win-x64`, the build system will look for C files in `./src/C/Drivers/x64/` first.
