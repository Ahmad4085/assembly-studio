; Hello, World
; Prints a message with DOS function 09h, then ends the program with function 4Ch.
; Build and run: the console shows "Hello, World!".

org 100h                    ; a .COM program starts at offset 100h

    mov dx, message         ; DS:DX = address of the text
    mov ah, 09h             ; DOS function 09h: print text up to the '$'
    int 21h

    mov ax, 4C00h           ; DOS function 4Ch: end the program, exit code 0 (AL)
    int 21h

message db 'Hello, World!', 13, 10, '$'   ; 13, 10 = carriage return, line feed
