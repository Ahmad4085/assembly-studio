; Conditional jumps
; CMP subtracts without keeping the result; it only sets the flags. A conditional jump then reads the flags.
; JB and JA compare unsigned numbers; JL and JG compare signed numbers.

org 100h

    mov ax, 3
    mov bx, 7
    call compare
    mov ax, 7
    mov bx, 7
    call compare
    mov ax, 9
    mov bx, 2
    call compare

    ; The same bits can mean different numbers: FFFFh is 65535 unsigned but -1 signed.
    mov dx, signed_line
    call print
    mov ax, 0FFFFh
    cmp ax, 1
    mov dx, unsigned_above  ; MOV doesn't change the flags
    ja unsigned_done        ; JA: above (unsigned): 65535 > 1
    mov dx, unsigned_below
unsigned_done:
    call print
    mov ax, 0FFFFh
    cmp ax, 1
    mov dx, signed_less
    jl signed_done          ; JL: less (signed): -1 < 1
    mov dx, signed_greater
signed_done:
    call print

    mov ax, 4C00h
    int 21h

; compare: prints "AX is less than / equal to / greater than BX" (unsigned).
compare:
    push ax
    call print_number
    pop ax
    cmp ax, bx
    mov dx, is_less
    jb .print               ; jump if below: AX < BX
    mov dx, is_equal
    je .print               ; jump if equal: AX = BX
    mov dx, is_greater      ; otherwise AX > BX
.print:
    call print
    mov ax, bx
    call print_number
    mov dx, new_line
    call print
    ret

; print_number: prints AX as an unsigned decimal number (keeps BX).
print_number:
    push bx
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
    pop bx
    ret

; print: prints the '$'-terminated text at DX.
print:
    mov ah, 09h
    int 21h
    ret

is_less        db ' is less than $'
is_equal       db ' is equal to $'
is_greater     db ' is greater than $'
signed_line    db 'FFFFh compared with 1: $'
unsigned_above db 'unsigned it is above (JA jumps), $'
unsigned_below db 'unsigned it is below (JB jumps), $'
signed_less    db 'signed it is less (JL jumps)', 13, 10, '$'
signed_greater db 'signed it is greater (JG jumps)', 13, 10, '$'
new_line       db 13, 10, '$'
