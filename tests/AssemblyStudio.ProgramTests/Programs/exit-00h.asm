; exit-00h.asm: ends with INT 21h AH=00h (AC-064 b)
org 100h
    mov ah, 00h
    int 21h
