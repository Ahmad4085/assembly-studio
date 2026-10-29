; 07: tries to switch the CPU level
org 100h
cpu 386
    pusha
    mov eax, 1
    ret
