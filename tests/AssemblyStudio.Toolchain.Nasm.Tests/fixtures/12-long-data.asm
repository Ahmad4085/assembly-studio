; 12: data longer than one listing row, reserved space, and code after it
org 100h
    mov dx, msg
    mov ah, 09h
    int 21h
    mov ax, 4C00h
    int 21h
msg db 'This string is long enough to wrap in the listing', 13, 10, '$'
table times 3 dw 1234h
after:
    nop
section .bss
buffer resb 64
