; 02: code and data sections, data referenced from code
org 100h
section .text
    mov dx, msg
    mov ah, 09h
    int 21h
    mov al, [count]
    mov ax, 4C00h
    int 21h
section .data
msg   db 'Hi$'
count db 5
table dw 1, 2, 3
section .text
    nop
