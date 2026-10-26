; 03: times and align
org 100h
    jmp short main
    times 5 nop
    align 16
main:
    mov cx, 2
    times 3 inc ax
    ret
section .data
pad times 4 db 0
    align 4
word1 dw 1234h
