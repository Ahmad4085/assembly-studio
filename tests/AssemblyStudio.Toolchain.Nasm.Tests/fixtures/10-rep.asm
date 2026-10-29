; 10: %rep blocks, including a nested one
org 100h
%assign i 0
%rep 3
    add ax, i
%assign i i+1
%endrep
%rep 2
  %rep 2
    inc cx
  %endrep
    inc dx
%endrep
    mov ax, 4C00h
    int 21h
