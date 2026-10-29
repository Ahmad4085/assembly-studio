; outer.inc: includes a file from a subfolder
outer:
    inc ax
%include "inc/sub/inner.inc"
    ret
