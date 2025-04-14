# PromethiumXS

PromethiumXS is a console that dose not exist this is an emulator for that console my goal right now is to get a full 3d game up and running 
For now were making its own custom assembly langauge most instructions are added im still working on controller stuff and instruction logic but its going along well


# EXAMPLE CODE


```; Test PROASM file to validate labels, jumps, calls, and returns.
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

```
