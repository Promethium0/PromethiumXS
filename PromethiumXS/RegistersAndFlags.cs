using System;

namespace PromethiumXS
{
    /// <summary>
    /// Represents the CPU status flags.
    /// </summary>
    [Flags]
    public enum CpuFlags : byte
    {
        None = 0x00,
        Zero = 0x01, // Set if the result of an operation is zero.
        Carry = 0x02, // Set on arithmetic carry or borrow.
        Overflow = 0x04, // Set if an arithmetic overflow occurs.
        Negative = 0x08, // Set if the result is negative.
        Greater= 0x10, // Set if the last comparison was greater than
        Less = 0x20, // Set if the last comparison was less than
        Equal = 0x40, // Set if the last comparison was equal
        NotEqual = 0x80, // Set if the last comparison was not equal
        GreaterOrEqual = Greater | Equal, // Set if the last comparison was greater than or equal to
        LessOrEqual = Less | Equal, // Set if the last comparison was less than or equal to
    }

    /// <summary>
    /// Represents graphics-related flags for tracking the 3D pipeline state.
    /// </summary>
    [Flags]
    public enum GfxFlags : byte
    {
        None = 0x00,
        BufferSwapPending = 0x01, // Indicates a pending frame buffer swap.
        RenderComplete = 0x02, // Indicates that rendering for the current frame has completed.
        Error = 0x04, // Signals a graphics processing error.
    }

    /// <summary>
    /// Contains the complete register set for the PromethiumXS console.
    /// </summary>
    public class PromethiumRegisters
    {
        /// <summary>
        /// The 32 general-purpose registers (R0 - R31) used for arithmetic, logic, and control flow.
        /// </summary>
        public int[] GPR { get; private set; }

        /// <summary>
       // 32 dedicated graphics registers (G0 - G31) used for graphics operations.
        /// </summary>
        public int[] Graphics { get; private set; }

        /// <summary>
        /// The CPU flags register.
        /// </summary>
        public CpuFlags CpuFlag { get; set; }

        /// <summary>
        /// The graphics flags register.
        /// </summary>
        public GfxFlags GraphicsFlag { get; set; }

        /// <summary>
        /// Initializes a new instance of the PromethiumRegisters class with default values.
        /// </summary>
        public PromethiumRegisters()
        {
            GPR = new int[32];        // 32 general-purpose registers.
            Graphics = new int[32];      // 32 dedicated graphics registers.
            CpuFlag = CpuFlags.None;    // Initialize all CPU flags as false.
            GraphicsFlag = GfxFlags.None; // Initialize graphics flags.
        }

        /// <summary>
        /// Resets all registers and flags to their default (zeroed/none) state.
        /// </summary>
        public void Reset()
        {
            Array.Clear(GPR, 0, GPR.Length);
            Array.Clear(Graphics, 0, Graphics.Length);
            CpuFlag = CpuFlags.None;
            GraphicsFlag = GfxFlags.None;
        }

        /// <summary>
        /// Dumps the current state of all registers and flags to the console (for debugging purposes).
        /// </summary>
        public void Dump()
        {
            Console.WriteLine("---- PromethiumXS Register Dump ----\n");
            Console.WriteLine("General Purpose Registers:");
            for (int i = 0; i < GPR.Length; i++)
            {
                Console.WriteLine($"R{i}: {GPR[i]}");
            }

            Console.WriteLine("\nGraphics Registers:");
            for (int i = 0; i < Graphics.Length; i++)
            {
                Console.WriteLine($"G{i}: {Graphics[i]}");
            }

            Console.WriteLine("\nCPU Flags: " + CpuFlag);
            Console.WriteLine("Graphics Flags: " + GraphicsFlag + "\n");
        }
    }
}
