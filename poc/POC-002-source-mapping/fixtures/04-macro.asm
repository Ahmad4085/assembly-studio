; 04: multi-line macro used twice
org 100h
%macro PRINT 1
    mov dx, %1
    mov ah, 09h
    int 21h
%endmacro
    PRINT msg1
    PRINT msg2
    mov ax, 4C00h
    int 21h
section .data
msg1 db 'A$'
msg2 db 'B$'
