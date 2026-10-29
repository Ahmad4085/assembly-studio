; exit-int20.asm: ends with INT 20h (AC-064 a)
org 100h
    mov dx, 1
    int 20h
