; Keyboard input
; DOS function 0Ah reads a line (Enter ends it); function 01h reads one key. Both show what is typed.
; Run it, type your name and press Enter, then press any key.

org 100h

    mov dx, ask_name
    call print
    mov dx, name_buffer
    mov ah, 0Ah             ; read a line into name_buffer
    int 21h
    ; name_buffer: byte 0 = room for 20 characters, byte 1 = characters typed, then the text and a carriage return.
    mov bl, [name_buffer + 1]
    mov bh, 0
    mov byte [name_buffer + 2 + bx], '$'    ; replace the carriage return with '$' so function 09h can print the name

    mov dx, greeting
    call print
    mov dx, name_buffer + 2
    call print
    mov dx, ask_key
    call print

    mov ah, 01h             ; read one key; AL = the character
    int 21h
    mov [key], al

    mov dx, you_pressed
    call print
    mov dl, [key]
    mov ah, 02h
    int 21h
    mov dx, new_line
    call print

    mov ax, 4C00h
    int 21h

; print: prints the '$'-terminated text at DX.
print:
    mov ah, 09h
    int 21h
    ret

ask_name    db 'What is your name? $'
greeting    db 13, 10, 'Hello, $'
ask_key     db '!', 13, 10, 'Press a key: $'
you_pressed db 13, 10, 'You pressed: $'
new_line    db 13, 10, '$'
key         db 0
name_buffer db 20, 0
            times 21 db 0
