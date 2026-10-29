; exit-ret.asm: ends with a near RET through the initial stack word (AC-064 d)
org 100h
    mov cx, 3
again:
    loop again
    ret
