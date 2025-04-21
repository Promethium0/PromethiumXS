using System.Windows.Forms;

public enum PromethiumOpcode : ushort
{
    // 0x00 - 0x1F: Basic Operations
    // 0x20 - 0x3F: Interrupts and I/O
    // 0x40 - 0x7F: Reserved for future use
    // 0x80 - 0x15A: Graphics and Rendering Operations
    // General Purpose Instructions

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
    IN = 0x1B, // Input from a peripheral device
    OUT = 0x1C, // Output to a register

    // example of in/out 
    //CHECK_INPUT:
    //
    //LI LEFT_STICK_LEFT R11
    //IN R11
    //JE MOVE_LEFT
    //
    //LI A_BUTTON R11
    //IN R11
    //JE JUMP
    //
    //MOVE_LEFT:
    //LI -5, R3; Set X velocity to -5
    //LI INPUT_RECEIVED, R11
    //OUT R11; Acknowledge input
    //RET
    //
    //



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

    //if your wondering why i had both the vector and normal instructions in the same enum is because its just easier sorry
    //also i tried doing it in a seprate thing and had to restart twice so im not doing that shit again
  

    // Basic vector operations
    VEC_INIT = 0xF8,      // Initialize a vector with x,y,z components
    VEC_COPY = 0xF9,      // Copy one vector to another
    VEC_ZERO = 0xFA,      // Set vector to zero (0,0,0)

    // Vector arithmetic
    VEC_ADD = 0xFB,       // Add two vectors
    VEC_SUB = 0xFC,       // Subtract two vectors
    VEC_MUL = 0xFD,       // Multiply vector by scalar
    VEC_DIV = 0xFE,       // Divide vector by scalar
    VEC_ADDI = 0x100,     // Add immediate value to vector components
    VEC_SUBI = 0x101,     // Subtract immediate value from vector components
    VEC_MULI = 0x102,     // Multiply vector components by immediate value
    VEC_DIVI = 0x103,     // Divide vector components by immediate value

    // Vector products and operations
    VEC_DOT = 0x104,      // Dot product of two vectors
    VEC_CROSS = 0x105,    // Cross product of two vectors
    VEC_LEN = 0x106,      // Calculate vector length/magnitude
    VEC_NORM = 0x107,     // Normalize vector (make unit length)

    // Matrix operations
    MAT_INIT = 0x108,     // Initialize a 4x4 transformation matrix
    MAT_IDENTITY = 0x109, // Set matrix to identity
    MAT_MUL = 0x10A,      // Multiply two matrices
    MAT_TRANSPOSE = 0x10B,// Transpose a matrix
    MAT_INVERSE = 0x10C,  // Invert a matrix

    // Transformation operations
    TRANSFORM = 0x10D,    // Transform a vector by a matrix
    TRANSLATE = 0x10E,    // Create translation matrix
    ROTATE_X = 0x10F,     // Create rotation matrix around X axis
    ROTATE_Y = 0x110,     // Create rotation matrix around Y axis
    ROTATE_Z = 0x111,     // Create rotation matrix around Z axis
    SCALE = 0x112,        // Create scaling matrix

    // Projection and camera operations
    PERSPECTIVE = 0x113,  // Create perspective projection matrix
    ORTHOGRAPHIC = 0x114, // Create orthographic projection matrix
    LOOKAT = 0x115,       // Create view matrix from eye, target, and up vectors

    // Rendering primitives
    DRAW_POINT = 0x116,   // Draw a single point
    DRAW_LINE = 0x117,    // Draw a line between two points
    DRAW_TRI = 0x118,     // Draw a triangle
    DRAW_QUAD = 0x119,    // Draw a quadrilateral
    DRAW_MESH = 0x11A,    // Draw a mesh (collection of triangles)

    // Clipping and culling
    CLIP_LINE = 0x11B,    // Clip a line to the view frustum
    CULL_FACE = 0x11C,    // Perform backface culling

    // Lighting and shading
    CALC_NORMAL = 0x11D,  // Calculate surface normal
    LIGHT_POINT = 0x11E,  // Apply point light to a surface
    LIGHT_DIR = 0x11F,    // Apply directional light to a surface
    SHADE_FLAT = 0x120,   // Apply flat shading
    SHADE_GOURAUD = 0x121,// Apply Gouraud (smooth) shading

    // Texture mapping
    TEX_COORD = 0x122,    // Set texture coordinates
    TEX_SAMPLE = 0x123,   // Sample texture at coordinates
    TEX_MAP = 0x124,      // Apply texture mapping to a primitive

    // Buffer operations
    CLEAR_BUFFER = 0x125, // Clear the frame buffer
    SWAP_BUFFER = 0x126,  // Swap front and back buffers
    Z_TEST = 0x127,       // Perform depth testing

    // Utility operations
    RAND_VEC = 0x128,     // Generate random vector
    LERP_VEC = 0x129,     // Linear interpolation between vectors
    BEZIER = 0x12A,       // Calculate point on Bezier curve

    // Display list operations
    DL_BEGIN = 0x12B,     // Begin recording a display list
    DL_END = 0x12C,       // End recording a display list
    DL_CALL = 0x12D,      // Execute a display list
    DL_DELETE = 0x12E,    // Delete a display list
    DL_COMPILE = 0x12F,   // Compile commands into an optimized display list
    DL_BIND = 0x130,      // Bind a display list to a name/ID
    DL_APPEND = 0x131,    // Append commands to an existing display list
    DL_CONDITIONAL = 0x132,// Conditionally execute parts of a display list

    // Batch Processing Instructions
    BATCH_VERTS = 0x133,  // Define multiple vertices in a single command
    BATCH_TRIS = 0x134,   // Define multiple triangles using vertex indices
    BATCH_QUADS = 0x135,  // Define multiple quads using vertex indices

    // Memory Management
    VERTS_LOAD = 0x136,   // Load vertices from memory into registers
    VERTS_STORE = 0x137,  // Store vertices from registers to memory
    VERTS_DMA = 0x138,    // DMA transfer of vertex data

    // State Management
    STATE_PUSH = 0x139,   // Push current rendering state onto stack
    STATE_POP = 0x13A,    // Pop rendering state from stack
    STATE_SET = 0x13B,    // Set a specific rendering state parameter

    // Culling and Clipping Optimizations
    FRUSTUM_TEST = 0x13C, // Test if object bounding volume is in view frustum
    OCCLUDE_TEST = 0x13D, // Test if object is occluded by previously rendered objects

    // Level of Detail
    LOD_SELECT = 0x13E,   // Select level of detail based on distance
    LOD_BIAS = 0x13F,     // Adjust LOD bias

    // Hierarchical Transformations
    PUSH_MATRIX = 0x140,  // Push current transformation matrix onto stack
    POP_MATRIX = 0x141,   // Pop transformation matrix from stack

    // Instancing
    INSTANCE_BEGIN = 0x142,// Begin instance definition
    INSTANCE_END = 0x143,  // End instance definition
    INSTANCE_DRAW = 0x144, // Draw multiple instances with different transforms

    // Vertex Attribute Management
    ATTRIB_COLOR = 0x145,  // Set vertex color attribute
    ATTRIB_NORMAL = 0x146, // Set vertex normal attribute
    ATTRIB_TEXCOORD = 0x147,// Set vertex texture coordinate attribute

    // Shader-like Operations
    SHADER_BIND = 0x148,   // Bind a shader program (simplified version)
    SHADER_PARAM = 0x149,  // Set shader parameters

    // Optimized Primitive Drawing
    DRAW_STRIP_TRI = 0x14A,// Draw triangle strip
    DRAW_STRIP_QUAD = 0x14B,// Draw quad strip
    DRAW_FAN = 0x14C,      // Draw triangle fan

    // Memory-Efficient Rendering
    VERT_CACHE_HINT = 0x14D,// Provide hint for vertex cache optimization

    // Specialized 3D Operations
    BILLBOARD = 0x14E,     // Create billboard that always faces camera
    PARTICLE_EMIT = 0x14F, // Emit particles (for particle systems)

    // Visibility and Sorting
    DEPTH_SORT = 0x150,    // Sort transparent objects by depth
    PRIORITY_SET = 0x151,  // Set rendering priority

    // Advanced Transformations
    QUATERNION_ROT = 0x152,// Rotation using quaternions (more efficient)

    // Specialized Rendering Techniques
    SHADOW_PROJ = 0x153,   // Create shadow projection matrix
    REFLECTION_MATRIX = 0x154,// Create reflection matrix

    // Performance Optimizations
    CACHE_FLUSH = 0x155,   // Flush vertex/index caches
    HINT_STATIC = 0x156,   // Hint that geometry is static
    HINT_DYNAMIC = 0x157,  // Hint that geometry changes frequently

    // Debugging
    DEBUG_MARKER = 0x158,  // Insert debug marker in display list
    PERF_BEGIN = 0x159,    // Begin performance measurement section
    PERF_END = 0x15A,       // End performance measurement section

    //Controller inputs
    A_BUTTON = 0x200, 
    B_BUTTON = 0x201,
    X_BUTTON = 0x202,
    Y_BUTTON = 0x203,
    L_BUTTON = 0x204,
    R_BUTTON = 0x205,
    ZL_BUTTON = 0x206,
    ZR_BUTTON = 0x207,
    START_BUTTON = 0x208,
    SELECT_BUTTON = 0x209,
    INPUT_RECIEIVED = 0x20A,
    L_STICK_LEFT = 0x20B,
    L_STICK_RIGHT = 0x20C,
    L_STICK_UP = 0x20D,
    L_STICK_DOWN = 0x20E,
    R_STICK_LEFT = 0x20F,
    R_STICK_RIGHT = 0x210,
    R_STICK_UP = 0x211,
    R_STICK_DOWN = 0x212,
    DPAD_LEFT = 0x213,
    DPAD_RIGHT = 0x214,
    DPAD_UP = 0x215,
    DPAD_DOWN = 0x216,


    //more loading instructions because i forgot to add them

  












}









