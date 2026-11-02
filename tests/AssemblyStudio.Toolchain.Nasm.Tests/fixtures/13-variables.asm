; TASK-106 fixture: data labels for the Variables panel (FR-097)
[org 0x0100]
COUNT   equ 5                   ; a constant, not a variable
        jmp  start

align 16, db 0
vuid:   db 'BC123456789', 0     ; one data line
prefix: times 2 db 0            ; a times line and a db line
        db 0
align 16, db 0
lhsval: dw 0x04D2
rhsval: dw 0xDDD5
title:  db 'a string longer than one listing row', 0
table:
.row:   db 1, 2, 3              ; a local data label after a bare label
flag    db 7                    ; a label without a colon

start:
        mov  ax, [lhsval]
.again:
        dec  ax
        jnz  .again
        mov  ax, 0x4C00
        int  0x21

section .bss
buffer: resb 20
