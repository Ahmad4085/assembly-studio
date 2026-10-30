; The stack and procedures
; CALL pushes the return address and RET pops it. PUSH and POP save values on the stack (last in, first out).
; Arguments can be passed in registers or on the stack.

org 100h

    ; 1. An argument in a register: square(9) returns 81 in AX.
    mov dx, square_text
    call print
    mov ax, 9
    call square
    call print_number
    mov dx, new_line
    call print

    ; 2. Arguments on the stack: add_two(30, 12) returns 42 in AX.
    mov dx, add_text
    call print
    mov ax, 30
    push ax                 ; first argument
    mov ax, 12
    push ax                 ; second argument
    call add_two
    add sp, 4               ; remove the two arguments from the stack
    call print_number
    mov dx, new_line
    call print

    ; 3. Last in, first out: push the letters of STACK, then pop them in reverse order.
    mov dx, reverse_text
    call print
    mov si, word_text
    mov cx, 5
push_letters:
    mov al, [si]
    push ax                 ; the stack holds words: the letter is in the low byte
    inc si
    loop push_letters
    mov cx, 5
pop_letters:
    pop dx                  ; DL = the letter
    mov ah, 02h
    int 21h
    loop pop_letters
    mov dx, new_line
    call print

    mov ax, 4C00h
    int 21h

; square: AX = AX * AX. MUL multiplies AX by its operand; the result is in DX:AX.
square:
    push dx                 ; save DX: MUL changes it
    mul ax
    pop dx
    ret

; add_two: AX = first argument + second argument, both passed on the stack.
; After PUSH BP: [BP] = old BP, [BP+2] = return address, [BP+4] = second argument, [BP+6] = first argument.
add_two:
    push bp
    mov bp, sp
    mov ax, [bp + 6]
    add ax, [bp + 4]
    pop bp
    ret

; print_number: prints AX as an unsigned decimal number.
print_number:
    mov cx, 0
    mov bx, 10
.divide:
    mov dx, 0
    div bx
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
    ret

; print: prints the '$'-terminated text at DX.
print:
    mov ah, 09h
    int 21h
    ret

square_text  db 'square(9) = $'
add_text     db 'add_two(30, 12) = $'
reverse_text db 'STACK popped in reverse: $'
word_text    db 'STACK'
new_line     db 13, 10, '$'
