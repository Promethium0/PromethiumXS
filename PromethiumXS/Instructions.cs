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

    // Advanced 3D Graphics instructions
    GFX_BEGIN = 0x20, // Begin a 3D rendering session/frame
    GFX_CLEAR = 0x21, // Clear the frame buffer (color & depth) for a new frame
    GFX_SET_TRANS = 0x22, // Set/update the transformation matrix (model/view/projection)
    GFX_DRAW_TRI = 0x23, // Draw a triangle using supplied vertex data
    GFX_DRAW_QUAD = 0x24, // Draw a quadrilateral (can be split into two triangles)
    GFX_LOAD_TEX = 0x25, // Load texture data from main memory to video memory
    GFX_SET_LIGHT = 0x26, // Configure lighting (intensity, color, direction) for the scene
    GFX_SET_CAM = 0x27, // Set camera parameters (position, orientation, field of view)
    GFX_END = 0x28, // End the 3D rendering session (finalize the current frame)
    GFX_SWAP = 0x29, // Swap the frame buffers to update the display with the new frame

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
    STOREI = 0x3D  // Store a value to memory at an immediate offset
}
