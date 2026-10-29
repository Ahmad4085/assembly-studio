; hello.asm: prints a greeting and exits with code 0 (AC-022)
org 100h
    mov dx, message
    mov ah, 09h
    int 21h
    mov ax, 4C00h
    int 21h
message db 'Hello, World!', 13, 10, '$'
