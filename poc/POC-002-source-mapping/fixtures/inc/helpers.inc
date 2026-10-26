; helpers.inc: included procedure
newline:
    mov dl, 13
    mov ah, 02h
    int 21h
    ret
