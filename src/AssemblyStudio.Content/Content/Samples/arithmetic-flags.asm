; Arithmetic and flags
; ADD and SUB change AL and set the flags: CF (carry), ZF (zero), SF (sign), OF (overflow).
; Each line shows the 8-bit result in AL and the four flags right after the instruction.

org 100h

    mov dx, add_carry       ; 200 + 100 = 300 doesn't fit in 8 bits: AL keeps 300 - 256 = 44, CF = 1
    call print
    mov al, 200
    add al, 100
    call show_result

    mov dx, sub_zero        ; 5 - 5 = 0: ZF = 1
    call print
    mov al, 5
    sub al, 5
    call show_result

    mov dx, sub_borrow      ; 3 - 5 needs a borrow: CF = 1; AL = 254, which is -2 when signed: SF = 1
    call print
    mov al, 3
    sub al, 5
    call show_result

    mov dx, add_overflow    ; 127 + 1 = 128 is too big for a signed byte (-128 to 127): OF = 1, SF = 1
    call print
    mov al, 127
    add al, 1
    call show_result

    mov ax, 4C00h
    int 21h

; show_result: prints AL in decimal and the flags CF, ZF, SF and OF.
; CALL doesn't change the flags, and PUSHF reads them first, so this shows the flags of the last ADD or SUB.
; (The print routine above ran before the ADD or SUB, so it didn't disturb them.)
show_result:
    pushf                   ; copy the flags to the stack
    pop bx                  ; BX = the flags (CF is bit 0, ZF bit 6, SF bit 7, OF bit 11)
    mov ah, 0
    call print_number       ; prints AX (= AL here)
    mov dx, text_cf
    mov ax, 0001h
    call print_flag
    mov dx, text_zf
    mov ax, 0040h
    call print_flag
    mov dx, text_sf
    mov ax, 0080h
    call print_flag
    mov dx, text_of
    mov ax, 0800h
    call print_flag
    mov dx, new_line
    call print
    ret

; print_flag: prints the text at DX, then 1 if the flag bit in AX is set in BX, else 0.
print_flag:
    push ax
    call print
    pop ax
    mov dl, '0'
    test bx, ax
    jz .show
    mov dl, '1'
.show:
    mov ah, 02h             ; DOS function 02h: print the character in DL
    int 21h
    ret

; print_number: prints AX as an unsigned decimal number (keeps BX).
print_number:
    mov cx, 0               ; digits counted
    mov si, 10
.divide:
    mov dx, 0
    div si                  ; AX = AX / 10, DX = remainder (the last digit)
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

add_carry    db '200 + 100 = $'
sub_zero     db '5 - 5 = $'
sub_borrow   db '3 - 5 = $'
add_overflow db '127 + 1 = $'
text_cf      db '   CF=$'
text_zf      db ' ZF=$'
text_sf      db ' SF=$'
text_of      db ' OF=$'
new_line     db 13, 10, '$'
