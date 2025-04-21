using System;
using System.Collections.Generic;
using System.Text;

namespace PromethiumXS
{
    /// <summary>
    /// Memory domains for PromethiumXS, each with a specific purpose and size.
    /// </summary>
    public enum MemoryDomain
    {
        SystemCode,      // Main system memory for code execution
        SystemData,      // Main system memory for general data
        VideoMemory,     // Frame buffers and rendering data
        TextureMemory,   // Storage for texture assets
        DisplayList,     // Display list commands for the GPU
        AudioMemory,     // Audio samples and sound data
        CartridgeROM,    // Read-only game data from cartridge
        CartridgeSave,   // Writable save data on cartridge
        IORegisters,     // Memory-mapped hardware registers
        CacheMemory,     // High-speed cache for frequently accessed data
        ScratchPad       // Small, fast memory for temporary calculations
    }

    /// <summary>
    /// Represents the complete memory system for PromethiumXS with specialized domains.
    /// </summary>
    public class Memory
    {
        /// <summary>
        /// Defines the size of each memory domain in bytes.
        /// </summary>
        private readonly Dictionary<MemoryDomain, int> DomainSizes = new Dictionary<MemoryDomain, int>
{
    { MemoryDomain.SystemCode,   16 * 1024 * 1024 },  // 16 MB - Code execution
    { MemoryDomain.SystemData,   16 * 1024 * 1024 },  // 16 MB - General data
    { MemoryDomain.VideoMemory,   4 * 1024 * 1024 },  //  4 MB - Frame buffers
    { MemoryDomain.TextureMemory, 8 * 1024 * 1024 },  //  8 MB - Texture storage
    { MemoryDomain.DisplayList,   2 * 1024 * 1024 },  //  2 MB - GPU command lists
    { MemoryDomain.AudioMemory,   1 * 1024 * 1024 },  //  1 MB - Audio samples
    { MemoryDomain.CartridgeROM, 32 * 1024 * 1024 },  // 32 MB - Game ROM data
    { MemoryDomain.CartridgeSave, 1 * 1024 * 1024 },  //  1 MB - Save data
    { MemoryDomain.IORegisters,   4 * 1024 },         //  4 KB - Hardware registers
    { MemoryDomain.CacheMemory,  512 * 1024 },        // 512 KB - Fast cache
    { MemoryDomain.ScratchPad,    64 * 1024 }         //  64 KB - Ultra-fast scratchpad
};
        //in total we have 80mb of ram



        // systemram data starts at 0x1000000

        /// <summary>
        /// Gets the memory for each domain.
        /// </summary>
        public Dictionary<MemoryDomain, byte[]> Domains { get; private set; }

        /// <summary>
        /// Holds the size of the loaded program in the System domain.
        /// </summary>
        public int ProgramSize { get; set; } = 0;

        // Internal stack used for PUSH and POP operations.
        private Stack<int> stack = new Stack<int>();

        // Maximum stack size to prevent memory leaks
        private const int MaxStackSize = 1024;

        /// <summary>
        /// Initializes the memory system with all domains.
        /// </summary>
        public Memory()
        {
            Domains = new Dictionary<MemoryDomain, byte[]>();

            // Initialize each domain with its specific size
            foreach (var domain in DomainSizes.Keys)
            {
                Domains[domain] = new byte[DomainSizes[domain]];
                Console.WriteLine($"[Memory] Initialized {domain} with {FormatSize(DomainSizes[domain])}");
            }
        }

        /// <summary>
        /// Formats a byte size into a human-readable string.
        /// </summary>
        private string FormatSize(int bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB" };
            int suffixIndex = 0;
            double size = bytes;

            while (size >= 1024 && suffixIndex < suffixes.Length - 1)
            {
                size /= 1024;
                suffixIndex++;
            }

            return $"{size:F2} {suffixes[suffixIndex]}";
        }

        /// <summary>
        /// Reads a byte from the specified memory domain at the given offset.
        /// </summary>
        public byte Read(MemoryDomain domain, int address)
        {
            ValidateAccess(domain, address);
            return Domains[domain][address];
        }

        /// <summary>
        /// Reads a 32-bit integer from the specified memory domain at the given offset.
        /// </summary>
        public int ReadInt(MemoryDomain domain, int address)
        {
            ValidateAccess(domain, address, 4);
            return BitConverter.ToInt32(Domains[domain], address);
        }

        /// <summary>
        /// Reads a 32-bit float from the specified memory domain at the given offset.
        /// </summary>
        public float ReadFloat(MemoryDomain domain, int address)
        {
            ValidateAccess(domain, address, 4);
            return BitConverter.ToSingle(Domains[domain], address);
        }

        /// <summary>
        /// Writes a byte to the specified memory domain at the given offset.
        /// </summary>
        public void Write(MemoryDomain domain, int address, byte value)
        {
            ValidateAccess(domain, address);

            // Prevent writing to ROM areas
            if (domain == MemoryDomain.CartridgeROM)
            {
                Console.WriteLine("[Memory] Warning: Attempted write to CartridgeROM ignored");
                return;
            }

            Domains[domain][address] = value;
        }

        /// <summary>
        /// Writes a 32-bit integer to the specified memory domain at the given offset.
        /// </summary>
        public void WriteInt(MemoryDomain domain, int address, int value)
        {
            ValidateAccess(domain, address, 4);

            // Prevent writing to ROM areas
            if (domain == MemoryDomain.CartridgeROM)
            {
                Console.WriteLine("[Memory] Warning: Attempted write to CartridgeROM ignored");
                return;
            }

            byte[] bytes = BitConverter.GetBytes(value);
            for (int i = 0; i < 4; i++)
            {
                Domains[domain][address + i] = bytes[i];
            }
        }

        /// <summary>
        /// Writes a 32-bit float to the specified memory domain at the given offset.
        /// </summary>
        public void WriteFloat(MemoryDomain domain, int address, float value)
        {
            ValidateAccess(domain, address, 4);

            // Prevent writing to ROM areas
            if (domain == MemoryDomain.CartridgeROM)
            {
                Console.WriteLine("[Memory] Warning: Attempted write to CartridgeROM ignored");
                return;
            }

            byte[] bytes = BitConverter.GetBytes(value);
            for (int i = 0; i < 4; i++)
            {
                Domains[domain][address + i] = bytes[i];
            }
        }

        /// <summary>
        /// Validates memory access to ensure it's within bounds.
        /// </summary>
        private void ValidateAccess(MemoryDomain domain, int address, int size = 1)
        {
            if (!Domains.ContainsKey(domain))
                throw new ArgumentException($"Invalid memory domain: {domain}");

            if (address < 0 || address + size > DomainSizes[domain])
                throw new ArgumentOutOfRangeException(nameof(address),
                    $"Address {address} is out of range for domain {domain} (size: {DomainSizes[domain]})");
        }

        /// <summary>
        /// Copies a block of data from one memory location to another.
        /// </summary>
        public void CopyBlock(MemoryDomain sourceDomain, int sourceAddress,
                             MemoryDomain destDomain, int destAddress, int length)
        {
            ValidateAccess(sourceDomain, sourceAddress, length);
            ValidateAccess(destDomain, destAddress, length);

            // Prevent writing to ROM areas
            if (destDomain == MemoryDomain.CartridgeROM)
            {
                Console.WriteLine("[Memory] Warning: Attempted write to CartridgeROM ignored");
                return;
            }

            // Use Buffer.BlockCopy for efficient memory copying
            Buffer.BlockCopy(Domains[sourceDomain], sourceAddress,
                            Domains[destDomain], destAddress, length);

            Console.WriteLine($"[Memory] Copied {length} bytes from {sourceDomain}:{sourceAddress} to {destDomain}:{destAddress}");
        }

        /// <summary>
        /// Resets all memory domains by clearing their contents.
        /// Also clears the program size and internal stack.
        /// </summary>
        public void Reset()
        {
            foreach (var domain in Domains.Keys)
            {
                // Don't clear CartridgeROM during reset
                if (domain != MemoryDomain.CartridgeROM)
                {
                    Array.Clear(Domains[domain], 0, DomainSizes[domain]);
                }
            }
            ProgramSize = 0;
            stack.Clear();
            Console.WriteLine("[Memory] All memory domains reset");
        }

        /// <summary>
        /// Dumps the content of a given memory domain for debugging.
        /// Displays 16 bytes per line in hexadecimal format.
        /// </summary>
        public void DumpDomain(MemoryDomain domain, int startAddress = 0, int length = 256)
        {
            if (!Domains.ContainsKey(domain))
                throw new ArgumentException("Invalid memory domain");

            // Adjust length to not exceed domain size
            length = Math.Min(length, DomainSizes[domain] - startAddress);
            if (length <= 0)
                return;

            Console.WriteLine($"Memory Dump for {domain} Domain (from 0x{startAddress:X8}, {length} bytes):");
            byte[] data = Domains[domain];

            StringBuilder hexLine = new StringBuilder();
            StringBuilder asciiLine = new StringBuilder();

            for (int i = startAddress; i < startAddress + length; i++)
            {
                // Start a new line every 16 bytes
                if ((i - startAddress) % 16 == 0)
                {
                    if (i > startAddress)
                    {
                        Console.WriteLine($"{hexLine} | {asciiLine}");
                        hexLine.Clear();
                        asciiLine.Clear();
                    }
                    hexLine.Append($"{i:X8}: ");
                }

                hexLine.Append($"{data[i]:X2} ");

                // For ASCII representation, only show printable characters
                char c = (char)data[i];
                asciiLine.Append(c >= 32 && c <= 126 ? c : '.');
            }

            // Print the last line if it's not complete
            if (hexLine.Length > 0)
            {
                // Pad the hex line to align the ASCII part
                while (hexLine.Length < 8 + 16 * 3)
                    hexLine.Append(' ');

                Console.WriteLine($"{hexLine} | {asciiLine}");
            }
        }

        /// <summary>
        /// Pushes an integer value onto the internal stack.
        /// </summary>
        public void Push(int value)
        {
            if (stack.Count >= MaxStackSize)
                throw new InvalidOperationException($"[Memory] Stack overflow: Maximum size of {MaxStackSize} reached");

            stack.Push(value);
            Console.WriteLine($"[Memory] Pushed value {value} onto the stack (depth: {stack.Count})");
        }

        /// <summary>
        /// Pops an integer value from the internal stack.
        /// </summary>
        /// <returns>The integer value popped from the stack.</returns>
        public int Pop()
        {
            if (stack.Count == 0)
                throw new InvalidOperationException("[Memory] Stack underflow: No values to pop");

            int value = stack.Pop();
            Console.WriteLine($"[Memory] Popped value {value} from the stack (depth: {stack.Count})");
            return value;
        }

        /// <summary>
        /// Peeks at the top value on the stack without removing it.
        /// </summary>
        public int Peek()
        {
            if (stack.Count == 0)
                throw new InvalidOperationException("[Memory] Stack is empty: Cannot peek");

            return stack.Peek();
        }

        /// <summary>
        /// Gets the current stack depth.
        /// </summary>
        public int StackDepth => stack.Count;

        /// <summary>
        /// Gets the total memory size across all domains.
        /// </summary>
        public long TotalMemorySize
        {
            get
            {
                long total = 0;
                foreach (var size in DomainSizes.Values)
                {
                    total += size;
                }
                return total;
            }
        }
    }
}
