using System.Windows.Forms;

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

    // Floating-point operations (these are also used for the gpu!)
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
    FMOD = 0x6F,  // Floating-point modulo






    //Conversion operations 
    ITOF = 0xA0, // Convert Integer to Float
    FTOI = 0xA1, // Convert Float to Integer

    //Display lists
    DLSTART = 0xB0, // Start Display List
    DLEND = 0xB1, // End Display List
    DLCALL = 0xB2, // Call Display List
    DLVERTEX = 0xB3, // add vertex to dl
    DLCOLOR = 0xB4, // set color for verticies
    DLPRIMITIVE = 0xB5, // define primitive type
    //for moving models loaded in you use either the float instructions immediate instructions or just the normal register instructions the display list will be loaded into a gfx register

    STOREMODEL = 0xC0,
    LOADMODEL = 0xC1,

}

public enum InterruptType : byte
{

    Keyboard = 0x01, // probably not used
    Mouse = 0x02,// probably not used but if it was it would function the same the controller interrupt but for mouse inputs
    Controller = 0x03 // used (basically this enables the reciver to receive data from the controller and then you use the in and out instructions to get the data)
}



public enum INOUTType : byte //these are commands sent to and from the controller 
{
    
    INPUT_RECEIVED = 0x01, // tells the controller that the input it provided was received
    INPUT_NOT_RECEIVED = 0x02, // tells the controller that the input it provided was not received (should probably never happen but if it does it will be used)
    LEFT_STICK_UP = 0x03, // when the left stick is moved up this triggers
    LEFT_STICK_DOWN = 0x04, // when the left stick is moved down this triggers
    LEFT_STICK_LEFT = 0x05, // when the left stick is moved left this triggers
    LEFT_STICK_RIGHT = 0x06, // when the left stick is moved right this triggers
    RIGHT_STICK_UP = 0x07, // when the right stick is moved up this triggers
    RIGHT_STICK_DOWN = 0x08, // when the right stick is moved down this triggers
    RIGHT_STICK_LEFT = 0x09, // when the right stick is moved left this triggers
    RIGHT_STICK_RIGHT = 0x0A, // when the right stick is moved right this triggers
    LEFT_TRIGGER = 0x0B, // when the left trigger is pressed this triggers
    RIGHT_TRIGGER = 0x0C, // when the right trigger is pressed this triggers
    A_BUTTON = 0x0D, // when the A button is pressed this triggers
    B_BUTTON = 0x0E, // when the B button is pressed this triggers
    X_BUTTON = 0x0F, // when the X button is pressed this triggers
    Y_BUTTON = 0x10, // when the Y button is pressed this triggers
    START_BUTTON = 0x11, // when the start button is pressed this triggers
    SELECT_BUTTON = 0x12, // when the select button is pressed this triggers



}



/// <summary>
/// so for controller inputs the in and out commands work like this IN (lets say we use the a button) A_BUTTON then we press enter and then we have a JE jump to a function
/// however if the a button is not pressed it dosent jump to the function and instead just continues on to the next instruction
/// or it dose a JNE jump to a function if the a button is not pressed
