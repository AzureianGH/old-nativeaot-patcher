DOTNET = dotnet
ARGS = build
CSPROJ = Kernel.csproj

QEMU = qemu-system-x86_64
QEMU_OUTPUT = ..\..\artifacts\bin\Kernel\debug\cosmos\Kernel.iso
QEMU_ARGS = -cdrom $(QEMU_OUTPUT)

all: build run

# Build the kernel
win:
	rm -rf ..\..\artifacts\obj\Kernel\debug\cosmos\asm
	rm -rf ..\..\artifacts\obj\Kernel\debug\cosmos\cobj
	$(DOTNET) $(ARGS) $(CSPROJ)
	

# Run the kernel (requires build)
win-run: win
	$(QEMU) $(QEMU_ARGS)
win-run-d: win
	$(QEMU) $(QEMU_ARGS) -s -S

win-verbose:
	$(DOTNET) $(ARGS) $(CSPROJ) --verbosity detailed

win-verbose-run: win-verbose
	$(QEMU) $(QEMU_ARGS)

# Clean the build
clean:
	$(DOTNET) clean $(CSPROJ)
	rm -rf ./bin/
	rm -rf ./obj/
	rm -rf ./output/

.PHONY: win win-run clean