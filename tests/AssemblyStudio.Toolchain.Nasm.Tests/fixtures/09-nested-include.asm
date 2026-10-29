; 09: nested includes in subfolders
org 100h
    call outer
    call inner
    mov ax, 4C00h
    int 21h
%include "inc/outer.inc"
    nop
