; 05: %include of a second file
org 100h
    call newline
    mov ax, 4C00h
    int 21h
%include "inc/helpers.inc"
    nop
