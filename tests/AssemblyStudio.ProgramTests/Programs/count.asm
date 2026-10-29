; count.asm: an endless loop, for the instruction budget
org 100h
again:
    inc ax
    jmp again
