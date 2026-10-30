; Loops
; LOOP decrements CX and jumps back while CX isn't 0. A loop can also count with DEC and JNZ.

org 100h

    mov dx, count_up
    call print
    mov cx, 10              ; repeat 10 times
    mov bx, 1               ; the number to print
    mov di, 0               ; the running sum
next_number:
    mov ax, bx
    call print_number
    mov dl, ' '
    mov ah, 02h
    int 21h
    add di, bx              ; sum = sum + number
    inc bx
    loop next_number        ; CX = CX - 1; jump while CX isn't 0

    mov dx, sum_is
    call print
    mov ax, di
    call print_number       ; 1 + 2 + ... + 10 = 55
    mov dx, new_line
    call print

    mov dx, count_down
    call print
    mov bx, 5
down:
    mov ax, bx
    call print_number
    mov dl, ' '
    mov ah, 02h
    int 21h
    dec bx                  ; DEC sets ZF when BX reaches 0
    jnz down                ; jump while BX isn't 0
    mov dx, new_line
    call print

    mov ax, 4C00h
    int 21h

; print_number: prints AX as an unsigned decimal number (keeps BX, CX and DI).
print_number:
    push cx
    mov cx, 0
    mov si, 10
.divide:
    mov dx, 0
    div si                  ; AX = AX / 10, DX = the last digit
    push dx
    inc cx
    cmp ax, 0
    jne .divide
.digit:
    pop dx
    add dl, '0'
    mov ah, 02h
    int 21h
    loop .digit
    pop cx
    ret

; print: prints the '$'-terminated text at DX.
print:
    mov ah, 09h
    int 21h
    ret

count_up   db 'Counting with LOOP: $'
sum_is     db 13, 10, 'Sum of 1 to 10: $'
count_down db 'Counting down with DEC and JNZ: $'
new_line   db 13, 10, '$'
