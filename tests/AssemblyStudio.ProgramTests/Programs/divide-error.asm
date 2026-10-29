; divide-error.asm: divides by zero (AC-031)
org 100h
    mov ax, 100
    mov bl, 0
    div bl                      ; line 5: divide error
    mov ax, 4C00h
    int 21h
