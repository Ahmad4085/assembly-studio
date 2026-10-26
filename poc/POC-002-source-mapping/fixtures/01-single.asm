; 01: single section, typical beginner program
org 100h
start:
    mov ax, 3
    add ax, 4
    cmp ax, 7
    jne start
    mov ah, 4Ch
    int 21h
