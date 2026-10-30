; String instructions
; MOVSB, LODSB, STOSB, SCASB and CMPSB work on bytes at DS:SI (source) and ES:DI (destination) and step SI and DI.
; CLD makes them step forward. REP repeats an instruction CX times; REPE and REPNE also stop on a comparison.

org 100h

    cld                     ; step forward through memory

    ; 1. Copy with REP MOVSB.
    mov si, source
    mov di, copy
    mov cx, source_length + 1   ; the text and its '$'
    rep movsb
    mov dx, copied_text
    call print
    mov dx, copy
    call print
    mov dx, new_line
    call print

    ; 2. Convert to upper case with LODSB and STOSB.
    mov si, copy
    mov di, copy
upper_next:
    lodsb                   ; AL = [SI], SI = SI + 1
    cmp al, 'a'
    jb store
    cmp al, 'z'
    ja store
    sub al, 'a' - 'A'       ; a lower-case letter: make it upper case
store:
    stosb                   ; [DI] = AL, DI = DI + 1
    cmp al, '$'
    jne upper_next
    mov dx, upper_text
    call print
    mov dx, copy
    call print
    mov dx, new_line
    call print

    ; 3. Find the length by scanning for '$' with REPNE SCASB.
    mov di, source
    mov al, '$'
    mov cx, 0FFFFh
    repne scasb             ; repeat while [DI] isn't '$'
    mov ax, 0FFFFh - 1
    sub ax, cx              ; the number of characters before the '$'
    push ax                 ; keep it: printing the label changes AX
    mov dx, length_text
    call print
    pop ax
    call print_number
    mov dx, new_line
    call print

    ; 4. Compare with REPE CMPSB: it stops at the first difference.
    mov dx, compare_text
    call print
    mov si, first
    mov di, second
    mov cx, 3
    repe cmpsb
    je same                 ; ZF still set: all three bytes matched
    mov ax, 3
    sub ax, cx              ; the position (1 to 3) of the first difference
    push ax
    mov dx, differ_text
    call print
    pop ax
    call print_number
    jmp compared
same:
    mov dx, same_text
    call print
compared:
    mov dx, new_line
    call print

    mov ax, 4C00h
    int 21h

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

source        db 'hello, string instructions'
source_length equ $ - source
              db '$'
copy          times source_length + 1 db 0
first         db 'ABC'
second        db 'ABD'
copied_text   db 'Copied: $'
upper_text    db 'Upper case: $'
length_text   db 'Length found with SCASB: $'
compare_text  db 'ABC and ABD: $'
differ_text   db 'first difference at position $'
same_text     db 'the same$'
new_line      db 13, 10, '$'
