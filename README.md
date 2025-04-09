# PromethiumXS

PromethiumXS is a console that dose not exist this is an emulator for that console my goal right now is to get a full 3d game up and running im able to add to registers and jump to labels


#EXAMPLE CODE
(this file is provided to you in the repo titled LABELTEST.PROASM)

; Test PROASM file to validate labels, jumps, calls, and returns.
START:
    MOV 10 R1          ; Set R1 = 10
    ADDI 5 R1          ; Add 5 to R1 (R1 = 15)
    CALL SUBROUTINE ;call the subroutine function
    JMP END            ; Jump to the END label

SUBROUTINE:
    ADDI 20 R1         ; Add 20 to R1 (R1 = R1 + 20)
    MOV 5 R2           ; Set R2 = 5
    RET                ; Return to the caller

END:
    HLT                ; Halt the program

