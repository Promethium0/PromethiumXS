using System;
using System.Numerics;


namespace PromethiumXS
{
    /// <summary>
    /// Represents the CPU status flags.
    /// </summary>
    [Flags]
    public enum CpuFlags : ushort
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
        Error = 0x100, // Set if an error has occurred.
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
        Model = 2,
        Memory = 3
    }

    /// <summary>
    /// Represents a register value that can be interpreted as integer, float, model or memory address
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

        // Memory address accessor with domain and offset
        private MemoryDomain _memoryDomain;
        private int _memoryOffset;

        public MemoryDomain MemoryDomain
        {
            get => _memoryDomain;
            set => _memoryDomain = value;
        }

        public int MemoryOffset
        {
            get => _memoryOffset;
            set => _memoryOffset = value;
        }

        // Set memory address (domain and offset)
        public void SetMemoryAddress(MemoryDomain domain, int offset)
        {
            _memoryDomain = domain;
            _memoryOffset = offset;
            // Store the domain in the high byte and offset in the remaining bytes
            _intValue = ((int)domain << 24) | (offset & 0x00FFFFFF);
        }

        // Get memory address from int value
        public void GetMemoryAddressFromInt()
        {
            _memoryDomain = (MemoryDomain)(_intValue >> 24);
            _memoryOffset = _intValue & 0x00FFFFFF;
        }

        // Constructors
        public RegisterValue() { _intValue = 0; }
        public RegisterValue(int value) { _intValue = value; }
        public RegisterValue(float value) { AsFloat = value; }
        public RegisterValue(string modelName) { AsModel = modelName; }
        public RegisterValue(MemoryDomain domain, int offset) { SetMemoryAddress(domain, offset); }

        public override string ToString()
        {
            if (AsModel != null)
                return AsModel; // Return the model name if set

            // If this is a memory address, format it appropriately
            if (_memoryDomain != 0 || _memoryOffset != 0)
                return $"{_memoryDomain}:0x{_memoryOffset:X6}";

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

        public Matrix4x4[] Matrices { get; private set; } // Existing property for matrices
        public Vector3[] Vectors { get; private set; }    // New property for vectors

        public CpuFlags CpuFlag { get; set; }
        public GfxFlags GraphicsFlag { get; set; }

        public PromethiumRegisters()
        {
            GPR = new RegisterValue[32];
            Graphics = new RegisterValue[32];
            GPRType = new RegisterType[32];
            GraphicsType = new RegisterType[32];
            Matrices = new Matrix4x4[16]; // Initialize with 16 matrices
            Vectors = new Vector3[16];   // Initialize with 16 vectors (adjust size as needed)

            for (int i = 0; i < GPR.Length; i++)
                GPR[i] = new RegisterValue();

            for (int i = 0; i < Graphics.Length; i++)
                Graphics[i] = new RegisterValue();

            for (int i = 0; i < Matrices.Length; i++)
                Matrices[i] = Matrix4x4.Identity; // Initialize matrices to identity

            for (int i = 0; i < Vectors.Length; i++)
                Vectors[i] = Vector3.Zero; // Initialize vectors to zero

            CpuFlag = CpuFlags.None;
            GraphicsFlag = GfxFlags.None;
        }

        public static class Matrix4x4Extensions
        {
            public static bool TryInvert(Matrix4x4 matrix, out Matrix4x4 result)
            {
                // Use the System.Numerics.Matrix4x4.Invert method
                if (Matrix4x4.Invert(matrix, out result))
                {
                    return true;
                }

                result = Matrix4x4.Identity; // Default to identity if inversion fails
                return false;
            }
        }

    }
}
    
