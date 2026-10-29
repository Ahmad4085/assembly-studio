; 08: macros nested 12 levels deep (listing levels <1> .. <12>)
org 100h
%macro L1 0
    inc ax
%endmacro
%macro L2 0
    L1
%endmacro
%macro L3 0
    L2
%endmacro
%macro L4 0
    L3
%endmacro
%macro L5 0
    L4
%endmacro
%macro L6 0
    L5
%endmacro
%macro L7 0
    L6
%endmacro
%macro L8 0
    L7
%endmacro
%macro L9 0
    L8
%endmacro
%macro L10 0
    L9
    inc bx
%endmacro
%macro L11 0
    L10
    inc cx
%endmacro
%macro L12 0
    L11
    inc dx
%endmacro
    L12
    nop
    L12
    mov ax, 4C00h
    int 21h
