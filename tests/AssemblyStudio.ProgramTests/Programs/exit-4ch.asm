; exit-4ch.asm: ends with INT 21h AH=4Ch and exit code 05h (AC-064 c)
org 100h
    mov ax, 4C05h
    int 21h
