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
    JGE = 0x50, // Jump if greater than or equal
    JLE = 0x51, // Jump if less than or equal



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

    // Floating-point operations
    FADD = 0x60,   // Floating-point addition
    FSUB = 0x61,   // Floating-point subtraction
    FMUL = 0x62,   // Floating-point multiplication
    FDIV = 0x63,   // Floating-point division
    FSQRT = 0x64,  // Square root
    FSIN = 0x65,   // Sine function
    FCOS = 0x66,   // Cosine function
    FTAN = 0x67,   // Tangent function
    MOVF = 0x68,  // Move float value between registers (easier than just adding that to mov)
    FAND = 0x69,  // Floating-point AND
    FOR = 0x6A,   // Floating-point OR
    FXOR = 0x6B,  // Floating-point XOR
    FNOT = 0x6C,  // Floating-point NOT
    FSHL = 0x6D,  // Floating-point shift left
    FSHR = 0x6E,  // Floating-point shift right
    FCMPEQ = 0x6F, // Floating-point compare for equality 


    //Conversion operations 
    ITOF = 0xA0, // Convert Integer to Float
    FTOI = 0xA1, // Convert Float to Integer



}

public enum InterruptType : byte
{

    Keyboard = 0x01, // probably not used
    Mouse = 0x02,// probably not used but if it was it would function the same the controller interrupt but for mouse inputs
    Controller = 0x03 // used (basically this enables the reciver to receive data from the controller and then you use the in and out instructions to get the data)
}
public enum GpuOpcodes : byte
{
    NOP = 0x00,
    LoadMicrocode = 0x01,
    DrawPixel = 0x02,
    DrawTriangle = 0x10,
    // draw triangle is basically all we need for now

}

