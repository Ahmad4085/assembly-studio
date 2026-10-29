; echo.asm: reads a line with INT 21h AH=0Ah and prints it back (AC-023)
org 100h
    mov dx, prompt
    mov ah, 09h
    int 21h
    mov dx, buffer
    mov ah, 0Ah
    int 21h
    mov dx, newline
    mov ah, 09h
    int 21h
    mov bl, [buffer + 1]        ; characters typed, without the CR
    mov bh, 0
    mov byte [buffer + 2 + bx], '$'
    mov dx, buffer + 2
    mov ah, 09h
    int 21h
    mov ax, 4C00h
    int 21h
prompt  db 'Name: $'
newline db 13, 10, '$'
buffer  db 20, 0
        times 21 db 0
