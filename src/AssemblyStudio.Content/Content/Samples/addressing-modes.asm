; Addressing modes
; The ways an instruction can name its data: a constant, a register, or memory at an address built from
; a displacement, BX or BP (base) and SI or DI (index).

org 100h

    mov ax, 7               ; immediate: the value is in the instruction
    mov dx, immediate_text
    call show

    mov bx, 7
    mov ax, bx              ; register: the value comes from another register
    mov dx, register_text
    call show

    mov ax, [numbers]       ; direct: the address is in the instruction
    mov dx, direct_text
    call show

    mov bx, numbers
    mov ax, [bx]            ; register indirect: the address is in BX
    mov dx, indirect_text
    call show

    mov bx, numbers
    mov ax, [bx + 4]        ; based: BX plus a displacement (the third word)
    mov dx, based_text
    call show

    mov si, 6
    mov ax, [numbers + si]  ; indexed: a displacement plus SI (the fourth word)
    mov dx, indexed_text
    call show

    mov bx, numbers
    mov si, 2
    mov ax, [bx + si + 6]   ; based indexed: BX plus SI plus a displacement (the fifth word)
    mov dx, based_indexed_text
    call show

    ; Sum all five words, stepping SI through the array.
    mov bx, numbers
    mov si, 0
    mov ax, 0
    mov cx, 5
sum_next:
    add ax, [bx + si]
    add si, 2               ; each word is 2 bytes
    loop sum_next
    mov dx, sum_text
    call show

    mov ax, 4C00h
    int 21h

; show: prints the text at DX, then AX in decimal and a new line.
show:
    push ax
    call print
    pop ax
    call print_number
    mov dx, new_line
    call print
    ret

; print_number: prints AX as an unsigned decimal number.
print_number:
    mov cx, 0
    mov di, 10
.divide:
    mov dx, 0
    div di
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

numbers            dw 10, 20, 30, 40, 50
immediate_text     db 'Immediate      mov ax, 7              AX = $'
register_text      db 'Register       mov ax, bx             AX = $'
direct_text        db 'Direct         mov ax, [numbers]      AX = $'
indirect_text      db 'Indirect       mov ax, [bx]           AX = $'
based_text         db 'Based          mov ax, [bx + 4]       AX = $'
indexed_text       db 'Indexed        mov ax, [numbers + si] AX = $'
based_indexed_text db 'Based indexed  mov ax, [bx + si + 6]  AX = $'
sum_text           db 'Sum of the five words:                AX = $'
new_line           db 13, 10, '$'
