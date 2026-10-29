; open-file.asm: asks DOS to create and then open a file, which the emulator does not support (AC-029)
org 100h
    mov dx, filename
    xor cx, cx
    mov ah, 3Ch                 ; create file: must halt, no host file is created
    int 21h
    mov ax, 3D00h               ; open file
    int 21h
    mov ax, 4C00h
    int 21h
filename db 'HARNESS.TXT', 0
