public enum PromethiumOpcode : byte
{
    // Basic operations
    NOP = 0x00, // No Operation
    MOV = 0x01, // Move data between registers/immediates

    // Memory operations
    LOAD = 0x02, // Load from memory into a register
    STORE = 0x03, // Store data from a register into memory

    // Arithmetic operations
    ADD = 0x04, // Addition
    SUB = 0x05, // Subtraction
    MUL = 0x06, // Multiplication
    DIV = 0x07, // Division
    MOD = 0x08, // Remainder/modulo

    // Bitwise operations
    AND = 0x09, // Bitwise AND
    OR = 0x0A, // Bitwise OR
    XOR = 0x0B, // Bitwise XOR
    NOT = 0x0C, // Bitwise NOT (complement)
    SHL = 0x0D, // Shift Left (logical)
    SHR = 0x0E, // Shift Right (logical)

    // Control flow operations
    CMP = 0x0F, // Compare two values
    JMP = 0x10, // Unconditional jump
    JZ = 0x11, // Jump if zero flag is set
    JNZ = 0x12, // Jump if zero flag is not set
    JE = 0x13, // Jump if equal (alias for JZ)
    JNE = 0x14, // Jump if not equal
    JG = 0x15, // Jump if greater than
    JL = 0x16, // Jump if less than


    // Subroutine operations
    CALL = 0x17, // Call subroutine
    RET = 0x18, // Return from subroutine

    // Stack operations
    PUSH = 0x19, // Push a value onto the stack
    POP = 0x1A, // Pop a value from the stack

    // I/O operations
    IN = 0x1B, // Read an input from a device/port
    OUT = 0x1C, // Write an output to a device/port

    // Special operations
    HLT = 0x1D, // Halt execution
    RAND = 0x1E, // Generate a random number into a register
    TIME = 0x1F, // Retrieve system time or tick count

    //Interrupt
    INT = 0x20, // Request an interrupt
    IRET = 0x21, // Return from an interrupt
    

    // Immediate operations
    ADDI = 0x30, // Add an immediate value to a register
    SUBI = 0x31, // Subtract an immediate value from a register
    MULI = 0x32, // Multiply a register by an immediate value
    DIVI = 0x33, // Divide a register by an immediate value
    ANDI = 0x34, // Bitwise AND with an immediate value
    ORI = 0x35, // Bitwise OR with an immediate value
    XORI = 0x36, // Bitwise XOR with an immediate value
    SHLI = 0x37, // Shift left by an immediate number of bits
    SHRI = 0x38, // Shift right by an immediate number of bits
    CMPI = 0x39, // Compare a register value with an immediate value
    LI = 0x3A, // Load an immediate value directly into a register
    MODI = 0x3B, // Modulo operation with an immediate value
    LOADI = 0x3C, // Load a value from memory at an immediate offset
    STOREI = 0x3D,  // Store a value to memory at an immediate offset

    //Other 
    EI = 0x3E, // Enable interrupts
    DI = 0x3F, // Disable interrupts

    
    
}

public enum InterruptType : byte
{

    Keyboard = 0x01, // probably not used
    Mouse = 0x02,// probably not used
    Controller = 0x03 // used
}
public enum GpuOpcodes : byte
{
    NOP = 0x00,
    LoadMicrocode = 0x01,
    TransformVertex = 0x02,
    DrawTriangle = 0x03,
    
}

