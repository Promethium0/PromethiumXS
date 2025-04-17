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
        Greater = 0x10, // Set if the last comparison was greater than
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
    /// Indicates whether a register is being used as integer, float, or model
    /// </summary>
    [Flags]
    public enum RegisterType : byte
    {
        Integer = 0,
        Float = 1,
        Model = 2 // New type for storing model names
    }

    /// <summary>
    /// Represents a register value that can be interpreted as either integer or float
    /// </summary>
    public class RegisterValue
    {
        private int _intValue;

        // Integer value accessor
        public int AsInt
        {
            get => _intValue;
            set => _intValue = value;
        }

        // Float value accessor (using bit conversion)
        public float AsFloat
        {
            get => BitConverter.ToSingle(BitConverter.GetBytes(_intValue), 0);
            set => _intValue = BitConverter.ToInt32(BitConverter.GetBytes(value), 0);
        }

        // Model name accessor (for storing display list references)
        public string AsModel { get; set; }

        // Constructors
        public RegisterValue() { _intValue = 0; }
        public RegisterValue(int value) { _intValue = value; }
        public RegisterValue(float value) { AsFloat = value; }
        public RegisterValue(string modelName) { AsModel = modelName; }

        public override string ToString()
        {
            if (AsModel != null)
                return AsModel; // Return the model name if set
            return _intValue.ToString();
        }
    }

    /// <summary>
    /// Contains the complete register set for the PromethiumXS console.
    /// </summary>
    public class PromethiumRegisters
    {
        public RegisterValue[] GPR { get; private set; }
        public RegisterValue[] Graphics { get; private set; }
        public RegisterType[] GPRType { get; private set; }
        public RegisterType[] GraphicsType { get; private set; }



        public CpuFlags CpuFlag { get; set; }
        public GfxFlags GraphicsFlag { get; set; }


        /// <summary>
        /// Initializes a new instance of the PromethiumRegisters class with default values.
        /// </summary>
        public PromethiumRegisters()
        {
            GPR = new RegisterValue[32];
            Graphics = new RegisterValue[32];
            GPRType = new RegisterType[32];
            GraphicsType = new RegisterType[32];

            for (int i = 0; i < GPR.Length; i++)
                GPR[i] = new RegisterValue();

            for (int i = 0; i < Graphics.Length; i++)
                Graphics[i] = new RegisterValue();

            CpuFlag = CpuFlags.None;
            GraphicsFlag = GfxFlags.None;
        }

        /// <summary>
        /// Resets all registers and flags to their default (zeroed/none) state.
        /// </summary>
        public void Reset()
        {
            for (int i = 0; i < GPR.Length; i++)
            {
                GPR[i] = new RegisterValue();
                GPRType[i] = RegisterType.Integer; // Reset to Integer type
            }

            for (int i = 0; i < Graphics.Length; i++)
            {
                Graphics[i] = new RegisterValue();
                GraphicsType[i] = RegisterType.Integer; // Reset to Integer type
            }

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
                string value = GPRType[i] == RegisterType.Float ?
                    GPR[i].AsFloat.ToString("F4") :
                    GPR[i].AsInt.ToString();
                Console.WriteLine($"R{i}: {value} ({GPRType[i]})");
            }

            Console.WriteLine("\nGraphics Registers:");
            for (int i = 0; i < Graphics.Length; i++)
            {
                string value = GraphicsType[i] == RegisterType.Float ?
                    Graphics[i].AsFloat.ToString("F4") :
                    Graphics[i].AsInt.ToString();
                Console.WriteLine($"G{i}: {value} ({GraphicsType[i]})");
            }

            Console.WriteLine("\nCPU Flags: " + CpuFlag);
            Console.WriteLine("Graphics Flags: " + GraphicsFlag + "\n");
        }
    }
}


