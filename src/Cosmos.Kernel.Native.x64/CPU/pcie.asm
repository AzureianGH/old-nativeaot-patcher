section .rodata
global pcieids_start
global pcieids_end
pcieids_start:
    incbin "pci.ids"
pcieids_end: