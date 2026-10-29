; 11: a macro that switches sections to emit data, then returns to .text
org 100h
%macro STR 2
section .data
%1 db %2, '$'
section .text
    mov dx, %1
%endmacro
section .text
    STR msg1, 'one'
    mov ah, 09h
    int 21h
    STR msg2, 'two'
    mov ax, 4C00h
    int 21h
