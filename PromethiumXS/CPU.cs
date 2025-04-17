using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using PromethiumXS;
namespace PromethiumXS
{


    
    public class Cpu
    {
        // Program Counter – holds the current instruction address.
        public int PC { get; private set; }

        // The CPU register file.
        public PromethiumRegisters Registers { get; private set; }
        // Reference to system memory.
        public Memory Memory { get; private set; }
        // Running flag for the main loop.
        public bool Running { get; private set; }

        // Simple call stack for subroutine calls.
        private List<int> callStack = new List<int>();

        private DisplayListManager _displayListManager;
        public DisplayListManager DisplayListManager => _displayListManager;

        private int _nextDplAddress = 0;







        public Cpu(Memory memory, PromethiumRegisters registers)
        {
            Memory = memory;
            Registers = registers;
            _displayListManager = new DisplayListManager(); // Initialize the DisplayListManager


            Reset();
        }

        /// <summary>
        /// Resets the CPU state: PC, registers, flags, and the call stack.
        /// </summary>
        public void Reset()
        {
            PC = 0;
            Running = true;
            callStack.Clear();

            // Reset general-purpose registers.
            for (int i = 0; i < Registers.GPR.Length; i++)
            {
                Registers.GPR[i].AsInt = 0;
            }
            // Reset graphics registers.
            for (int i = 0; i < Registers.Graphics.Length; i++)
            {
                Registers.Graphics[i].AsInt = 0;
            }

            // Clear flags.
            Registers.CpuFlag = CpuFlags.None;
            Registers.GraphicsFlag = GfxFlags.None;
        }

        /// <summary>
        /// Fetches a single byte from memory and increments PC.
        /// </summary>
        private byte FetchByte()
        {
            if (PC >= Memory.ProgramSize)
            {
                Console.WriteLine("[CPU] End of program reached during FetchByte(). Halting.");
                Running = false;
                return 0;
            }
            byte value = Memory.Read(MemoryDomain.System, PC);
            PC++;
            return value;
        }

        /// <summary>
        /// Fetches a 32-bit little–endian integer from memory and increments PC by 4.
        /// </summary>
        private int FetchInt()
        {
            if (PC + 3 >= Memory.ProgramSize)
            {
                Console.WriteLine("[CPU] End of program reached during FetchInt(). Halting.");
                Running = false;
                return 0;
            }
            int value = Memory.Read(MemoryDomain.System, PC)
                      | Memory.Read(MemoryDomain.System, PC + 1) << 8
                      | Memory.Read(MemoryDomain.System, PC + 2) << 16
                      | Memory.Read(MemoryDomain.System, PC + 3) << 24;
            PC += 4;
            return value;
        }

        private float FetchFloat()
        {
            // Read 4 bytes from memory and convert to float
            byte[] bytes = new byte[4];
            for (int i = 0; i < 4; i++)
            {
                bytes[i] = (byte)Memory.Read(MemoryDomain.System, PC + i);
            }
            PC += 4;
            // In your FetchFloat or similar method
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }


            // Convert the bytes to a float (IEEE 754 format)
            return BitConverter.ToSingle(bytes, 0);
        }

        /// <summary>
        /// Executes instructions until a HLT is encountered or end–of–program is reached.
        /// </summary>
        public void Run()
        {
            while (Running && PC < Memory.ProgramSize)
            {
                Step();
                
            }
            Running = false;
            Console.WriteLine("[CPU] Execution halted.");
        }

        /// <summary>
        /// Decodes and executes a single instruction.
        /// </summary>
        public void Step()
        {
            if (PC >= Memory.ProgramSize)
            {
                Console.WriteLine("[CPU] End of program reached. Halting.");
                Running = false;
                return;
            }

            byte opCodeByte = FetchByte();
            PromethiumOpcode opcode = (PromethiumOpcode)opCodeByte;
            Console.WriteLine($"[CPU] Executing {opcode} at PC: {PC - 1}");

            switch (opcode)
            {
                case PromethiumOpcode.NOP:
                    break;

                case PromethiumOpcode.MOV:
                    {
                        // Format: [opcode][dest register][immediate (4 bytes)]
                        int immediate = FetchInt();
                        byte destReg = FetchByte();

                        if (destReg < Registers.GPR.Length)
                        {
                            Registers.GPR[destReg] = new RegisterValue(immediate);
                            Console.WriteLine($"[CPU] MOV: Set R{destReg} = {immediate}");
                        }
                        else if (destReg < Registers.GPR.Length + Registers.Graphics.Length)
                        {
                            int graphicsIndex = destReg - Registers.GPR.Length;
                            Registers.Graphics[graphicsIndex] = new RegisterValue(immediate);
                            Console.WriteLine($"[CPU] MOV: Set G{graphicsIndex} = {immediate}");
                        }
                        else
                        {
                            Console.WriteLine($"[CPU] MOV: Invalid register index {destReg}");
                        }
                        break;
                    }
                case PromethiumOpcode.ADD:
                    {
                        // Format: [opcode][register1][register2]
                        byte regIndex1 = FetchByte();
                        byte regIndex2 = FetchByte();
                        if (regIndex1 < Registers.GPR.Length && regIndex2 < Registers.GPR.Length)
                        {
                            // Check if both registers are of float type.
                            if (Registers.GPRType[regIndex1] == RegisterType.Float &&
                                Registers.GPRType[regIndex2] == RegisterType.Float)
                            {
                                float before = Registers.GPR[regIndex1].AsFloat;
                                Registers.GPR[regIndex1].AsFloat += Registers.GPR[regIndex2].AsFloat;
                                Console.WriteLine($"[CPU] ADD (float): R{regIndex1} ({before}) + R{regIndex2} = {Registers.GPR[regIndex1]}");

                                if (Registers.GPR[regIndex1].AsFloat == 0)
                                    Registers.CpuFlag |= CpuFlags.Zero;
                                else
                                    Registers.CpuFlag &= ~CpuFlags.Zero;
                            }
                            else
                            {
                                // Default to integer addition.
                                int before = Registers.GPR[regIndex1].AsInt;
                                Registers.GPR[regIndex1].AsInt += Registers.GPR[regIndex2].AsInt;
                                Console.WriteLine($"[CPU] ADD (int): R{regIndex1} ({before}) + R{regIndex2} = {Registers.GPR[regIndex1]}");

                                if (Registers.GPR[regIndex1].AsInt == 0)
                                    Registers.CpuFlag |= CpuFlags.Zero;
                                else
                                    Registers.CpuFlag &= ~CpuFlags.Zero;
                            }
                        }
                        else if (regIndex1 < Registers.GPR.Length + Registers.Graphics.Length && regIndex2 < Registers.Graphics.Length)
                        {
                            int graphicsIndex1 = regIndex1 - Registers.GPR.Length;
                            int graphicsIndex2 = regIndex2 - Registers.Graphics.Length;
                            int before = Registers.Graphics[graphicsIndex1].AsInt;
                            Registers.Graphics[graphicsIndex1].AsInt += Registers.Graphics[graphicsIndex2].AsInt;
                            Console.WriteLine($"[CPU] ADD (graphics): G{graphicsIndex1} ({before}) + G{graphicsIndex2} = {Registers.Graphics[graphicsIndex1]}");
                        }
                        else
                        {
                            Console.WriteLine($"[CPU] ADD: Invalid register index {regIndex1} or {regIndex2}");
                        }
                        break;
                    }

                case PromethiumOpcode.SUB:
                    {
                        // Format: [opcode][register1][register2]
                        byte regIndex1 = FetchByte();
                        byte regIndex2 = FetchByte();
                        if (regIndex1 < Registers.GPR.Length && regIndex2 < Registers.GPR.Length)
                        {
                            int before = Registers.GPR[regIndex1].AsInt;
                            Registers.GPR[regIndex1].AsInt -= Registers.GPR[regIndex2].AsInt;
                            Console.WriteLine($"[CPU] SUB: R{regIndex1} ({before}) - R{regIndex2} = {Registers.GPR[regIndex1]}");
                            if (Registers.GPR[regIndex1].AsInt == 0)
                                Registers.CpuFlag |= CpuFlags.Zero;
                            else
                                Registers.CpuFlag &= ~CpuFlags.Zero;
                        }
                        else if (regIndex1 < Registers.GPR.Length + Registers.Graphics.Length && regIndex2 < Registers.Graphics.Length)
                        {
                            int graphicsIndex1 = regIndex1 - Registers.GPR.Length;
                            int graphicsIndex2 = regIndex2 - Registers.Graphics.Length;
                            int before = Registers.Graphics[graphicsIndex1].AsInt;
                            Registers.Graphics[graphicsIndex1].AsInt -= Registers.Graphics[graphicsIndex2].AsInt;
                            Console.WriteLine($"[CPU] SUB: G{graphicsIndex1} ({before}) - G{graphicsIndex2} = {Registers.Graphics[graphicsIndex1]}");
                        }
                        else
                        {
                            Console.WriteLine($"[CPU] SUB: Invalid register index {regIndex1} or {regIndex2}");
                        }
                        break;
                    }
                case PromethiumOpcode.MUL:
                    {
                        // Format: [opcode][register1][register2]
                        byte regIndex1 = FetchByte();
                        byte regIndex2 = FetchByte();
                        if (regIndex1 < Registers.GPR.Length && regIndex2 < Registers.GPR.Length)
                        {
                            int before = Registers.GPR[regIndex1].AsInt;
                            Registers.GPR[regIndex1].AsInt *= Registers.GPR[regIndex2].AsInt;
                            Console.WriteLine($"[CPU] MUL: R{regIndex1} ({before}) * R{regIndex2} = {Registers.GPR[regIndex1]}");
                            if (Registers.GPR[regIndex1].AsInt == 0)
                                Registers.CpuFlag |= CpuFlags.Zero;
                            else
                                Registers.CpuFlag &= ~CpuFlags.Zero;
                        }
                        else if (regIndex1 < Registers.GPR.Length + Registers.Graphics.Length && regIndex2 < Registers.Graphics.Length)
                        {
                            int graphicsIndex1 = regIndex1 - Registers.GPR.Length;
                            int graphicsIndex2 = regIndex2 - Registers.Graphics.Length;
                            int before = Registers.Graphics[graphicsIndex1].AsInt;
                            Registers.Graphics[graphicsIndex1].AsInt *= Registers.Graphics[graphicsIndex2].AsInt;
                            Console.WriteLine($"[CPU] MUL: G{graphicsIndex1} ({before}) * G{graphicsIndex2} = {Registers.Graphics[graphicsIndex1]}");
                        }
                        else
                        {
                            Console.WriteLine($"[CPU] MUL: Invalid register index {regIndex1} or {regIndex2}");
                        }
                        break;
                    }
                case PromethiumOpcode.DIV:
                    {
                        // Format: [opcode][register1][register2]
                        byte regIndex1 = FetchByte();
                        byte regIndex2 = FetchByte();
                        if (regIndex1 < Registers.GPR.Length && regIndex2 < Registers.GPR.Length)
                        {
                            if (Registers.GPR[regIndex2].AsInt != 0)
                            {
                                int before = Registers.GPR[regIndex1].AsInt;
                                Registers.GPR[regIndex1].AsInt /= Registers.GPR[regIndex2].AsInt;
                                Console.WriteLine($"[CPU] DIV: R{regIndex1} ({before}) / R{regIndex2} = {Registers.GPR[regIndex1]}");
                                if (Registers.GPR[regIndex1].AsInt == 0)
                                    Registers.CpuFlag |= CpuFlags.Zero;
                                else
                                    Registers.CpuFlag &= ~CpuFlags.Zero;
                            }
                            else
                            {
                                Console.WriteLine("[CPU] DIV: Division by zero error.");
                            }
                        }
                        else if (regIndex1 < Registers.GPR.Length + Registers.Graphics.Length && regIndex2 < Registers.Graphics.Length)
                        {
                            int graphicsIndex1 = regIndex1 - Registers.GPR.Length;
                            int graphicsIndex2 = regIndex2 - Registers.Graphics.Length;
                            int before = Registers.Graphics[graphicsIndex1].AsInt;
                            Registers.Graphics[graphicsIndex1].AsInt /= Registers.Graphics[graphicsIndex2].AsInt;
                            Console.WriteLine($"[CPU] DIV: G{graphicsIndex1} ({before}) / G{graphicsIndex2} = {Registers.Graphics[graphicsIndex1]}");
                        }
                        else
                        {
                            Console.WriteLine($"[CPU] DIV: Invalid register index {regIndex1} or {regIndex2}");
                        }
                        break;
                    }
                case PromethiumOpcode.AND:
                    {
                        // Format: [opcode][register1][register2]
                        byte regIndex1 = FetchByte();
                        byte regIndex2 = FetchByte();
                        if (regIndex1 < Registers.GPR.Length && regIndex2 < Registers.GPR.Length)
                        {
                            int before = Registers.GPR[regIndex1].AsInt;
                            Registers.GPR[regIndex1].AsInt &= Registers.GPR[regIndex2].AsInt;
                            Console.WriteLine($"[CPU] AND: R{regIndex1} ({before}) & R{regIndex2} = {Registers.GPR[regIndex1]}");
                            if (Registers.GPR[regIndex1].AsInt == 0)
                                Registers.CpuFlag |= CpuFlags.Zero;
                            else
                                Registers.CpuFlag &= ~CpuFlags.Zero;
                        }
                        else if (regIndex1 < Registers.GPR.Length + Registers.Graphics.Length && regIndex2 < Registers.Graphics.Length)
                        {
                            int graphicsIndex1 = regIndex1 - Registers.GPR.Length;
                            int graphicsIndex2 = regIndex2 - Registers.Graphics.Length;
                            int before = Registers.Graphics[graphicsIndex1].AsInt;
                            Registers.Graphics[graphicsIndex1].AsInt &= Registers.Graphics[graphicsIndex2].AsInt;
                            Console.WriteLine($"[CPU] AND: G{graphicsIndex1} ({before}) & G{graphicsIndex2} = {Registers.Graphics[graphicsIndex1]}");
                        }
                        else
                        {
                            Console.WriteLine($"[CPU] AND: Invalid register index {regIndex1} or {regIndex2}");
                        }
                        break;
                    }
                case PromethiumOpcode.OR:
                    {
                        // Format: [opcode][register1][register2]
                        byte regIndex1 = FetchByte();
                        byte regIndex2 = FetchByte();
                        if (regIndex1 < Registers.GPR.Length && regIndex2 < Registers.GPR.Length)
                        {
                            int before = Registers.GPR[regIndex1].AsInt;
                            Registers.GPR[regIndex1].AsInt |= Registers.GPR[regIndex2].AsInt;
                            Console.WriteLine($"[CPU] OR: R{regIndex1} ({before}) | R{regIndex2} = {Registers.GPR[regIndex1]}");
                            if (Registers.GPR[regIndex1].AsInt == 0)
                                Registers.CpuFlag |= CpuFlags.Zero;
                            else
                                Registers.CpuFlag &= ~CpuFlags.Zero;
                        }
                        else if (regIndex1 < Registers.GPR.Length + Registers.Graphics.Length && regIndex2 < Registers.Graphics.Length)
                        {
                            int graphicsIndex1 = regIndex1 - Registers.GPR.Length;
                            int graphicsIndex2 = regIndex2 - Registers.Graphics.Length;
                            int before = Registers.Graphics[graphicsIndex1].AsInt;
                            Registers.Graphics[graphicsIndex1].AsInt |= Registers.Graphics[graphicsIndex2].AsInt;
                            Console.WriteLine($"[CPU] OR: G{graphicsIndex1} ({before}) | G{graphicsIndex2} = {Registers.Graphics[graphicsIndex1]}");
                        }
                        else
                        {
                            Console.WriteLine($"[CPU] OR: Invalid register index {regIndex1} or {regIndex2}");
                        }
                        break;
                    }
                case PromethiumOpcode.XOR:
                    {
                        // Format: [opcode][register1][register2]
                        byte regIndex1 = FetchByte();
                        byte regIndex2 = FetchByte();
                        if (regIndex1 < Registers.GPR.Length && regIndex2 < Registers.GPR.Length)
                        {
                            int before = Registers.GPR[regIndex1].AsInt;
                            Registers.GPR[regIndex1].AsInt ^= Registers.GPR[regIndex2].AsInt;
                            Console.WriteLine($"[CPU] XOR: R{regIndex1} ({before}) ^ R{regIndex2} = {Registers.GPR[regIndex1]}");
                            if (Registers.GPR[regIndex1].AsInt == 0)
                                Registers.CpuFlag |= CpuFlags.Zero;
                            else
                                Registers.CpuFlag &= ~CpuFlags.Zero;
                        }
                        else if (regIndex1 < Registers.GPR.Length + Registers.Graphics.Length && regIndex2 < Registers.Graphics.Length)
                        {
                            int graphicsIndex1 = regIndex1 - Registers.GPR.Length;
                            int graphicsIndex2 = regIndex2 - Registers.Graphics.Length;
                            int before = Registers.Graphics[graphicsIndex1].AsInt;
                            Registers.Graphics[graphicsIndex1].AsInt ^= Registers.Graphics[graphicsIndex2].AsInt;
                            Console.WriteLine($"[CPU] XOR: G{graphicsIndex1} ({before}) ^ G{graphicsIndex2} = {Registers.Graphics[graphicsIndex1]}");
                        }
                        else
                        {
                            Console.WriteLine($"[CPU] XOR: Invalid register index {regIndex1} or {regIndex2}");
                        }
                        break;
                    }
                case PromethiumOpcode.NOT:
                    {
                        // Format: [opcode][register]
                        byte regIndex = FetchByte();
                        if (regIndex < Registers.GPR.Length)
                        {
                            int before = Registers.GPR[regIndex].AsInt;
                            Registers.GPR[regIndex].AsInt = ~Registers.GPR[regIndex].AsInt;

                            Console.WriteLine($"[CPU] NOT: R{regIndex} ({before}) = ~R{regIndex} = {Registers.GPR[regIndex]}");
                            if (Registers.GPR[regIndex].AsInt == 0)
                                Registers.CpuFlag |= CpuFlags.Zero;
                            else
                                Registers.CpuFlag &= ~CpuFlags.Zero;
                        }
                        else if (regIndex < Registers.GPR.Length + Registers.Graphics.Length)
                        {
                            int graphicsIndex = regIndex - Registers.GPR.Length;
                            int before = Registers.Graphics[graphicsIndex].AsInt;
                            Registers.Graphics[graphicsIndex].AsInt = ~Registers.Graphics[graphicsIndex].AsInt;
                            Console.WriteLine($"[CPU] NOT: G{graphicsIndex} ({before}) = ~G{graphicsIndex} = {Registers.Graphics[graphicsIndex]}");
                        }
                        else
                        {
                            Console.WriteLine($"[CPU] NOT: Invalid register index {regIndex}");
                        }
                        break;
                    }
                case PromethiumOpcode.SHL:
                    {
                        // Format: [opcode][register][shift amount (4 bytes)]
                        byte regIndex = FetchByte();
                        int shiftAmount = FetchInt();
                        if (regIndex < Registers.GPR.Length)
                        {
                            int before = Registers.GPR[regIndex].AsInt;
                            Registers.GPR[regIndex].AsInt <<= shiftAmount;
                            Console.WriteLine($"[CPU] SHL: R{regIndex} ({before}) << {shiftAmount} = {Registers.GPR[regIndex]}");
                            if (Registers.GPR[regIndex].AsInt == 0)
                                Registers.CpuFlag |= CpuFlags.Zero;
                            else
                                Registers.CpuFlag &= ~CpuFlags.Zero;
                        }
                        else if (regIndex < Registers.GPR.Length + Registers.Graphics.Length)
                        {
                            int graphicsIndex = regIndex - Registers.GPR.Length;
                            int before = Registers.Graphics[graphicsIndex].AsInt;
                            Registers.Graphics[graphicsIndex].AsInt <<= shiftAmount;
                            Console.WriteLine($"[CPU] SHL: G{graphicsIndex} ({before}) << {shiftAmount} = {Registers.Graphics[graphicsIndex]}");
                        }
                        else
                        {
                            Console.WriteLine($"[CPU] SHL: Invalid register index {regIndex}");
                        }
                        break;
                    }
                case PromethiumOpcode.SHR:
                    {
                        // Format: [opcode][register][shift amount (4 bytes)]
                        byte regIndex = FetchByte();
                        int shiftAmount = FetchInt();
                        if (regIndex < Registers.GPR.Length)
                        {
                            int before = Registers.GPR[regIndex].AsInt;
                            Registers.GPR[regIndex].AsInt >>= shiftAmount;
                            Console.WriteLine($"[CPU] SHR: R{regIndex} ({before}) >> {shiftAmount} = {Registers.GPR[regIndex]}");
                            if (Registers.GPR[regIndex].AsInt == 0)
                                Registers.CpuFlag |= CpuFlags.Zero;
                            else
                                Registers.CpuFlag &= ~CpuFlags.Zero;
                        }
                        else if (regIndex < Registers.GPR.Length + Registers.Graphics.Length)
                        {
                            int graphicsIndex = regIndex - Registers.GPR.Length;
                            int before = Registers.Graphics[graphicsIndex].AsInt;
                            Registers.Graphics[graphicsIndex].AsInt >>= shiftAmount;
                            Console.WriteLine($"[CPU] SHR: G{graphicsIndex} ({before}) >> {shiftAmount} = {Registers.Graphics[graphicsIndex]}");
                        }
                        else
                        {
                            Console.WriteLine($"[CPU] SHR: Invalid register index {regIndex}");
                        }
                        break;
                    }


                case PromethiumOpcode.ADDI:
                    {
                        // Format: [opcode][register][immediate (4 bytes)]
                        byte regIndex = FetchByte();
                        int immValue = FetchInt();
                        if (regIndex < Registers.GPR.Length)
                        {
                            int before = Registers.GPR[regIndex].AsInt;
                            Registers.GPR[regIndex].AsInt += immValue;
                            Console.WriteLine($"[CPU] ADDI: R{regIndex} ({before}) + {immValue} = {Registers.GPR[regIndex]}");

                            if (Registers.GPR[regIndex].AsInt == 0)
                                Registers.CpuFlag |= CpuFlags.Zero;
                            else
                                Registers.CpuFlag &= ~CpuFlags.Zero;
                        }
                        else if (regIndex < Registers.GPR.Length + Registers.Graphics.Length)
                        {
                            int graphicsIndex = regIndex - Registers.GPR.Length;
                            int before = Registers.Graphics[graphicsIndex].AsInt;
                            Registers.Graphics[graphicsIndex].AsInt += immValue;
                            Console.WriteLine($"[CPU] ADDI: G{graphicsIndex} ({before}) + {immValue} = {Registers.Graphics[graphicsIndex]}");
                        }
                        else
                        {
                            Console.WriteLine($"[CPU] ADDI: Invalid register index {regIndex}");
                        }
                        break;
                    }

                case PromethiumOpcode.SUBI:
                    {
                        // Format: [opcode][register][immediate (4 bytes)]
                        byte regIndex = FetchByte();
                        int immValue = FetchInt();
                        if (regIndex < Registers.GPR.Length)
                        {
                            int before = Registers.GPR[regIndex].AsInt;
                            Registers.GPR[regIndex].AsInt -= immValue;
                            Console.WriteLine($"[CPU] SUBI: R{regIndex} ({before}) - {immValue} = {Registers.GPR[regIndex]}");

                            if (Registers.GPR[regIndex].AsInt == 0)
                                Registers.CpuFlag |= CpuFlags.Zero;
                            else
                                Registers.CpuFlag &= ~CpuFlags.Zero;
                        }
                        else if (regIndex < Registers.GPR.Length + Registers.Graphics.Length)
                        {
                            int graphicsIndex = regIndex - Registers.GPR.Length;
                            int before = Registers.Graphics[graphicsIndex].AsInt;
                            Registers.Graphics[graphicsIndex].AsInt -= immValue;
                            Console.WriteLine($"[CPU] SUBI: G{graphicsIndex} ({before}) - {immValue} = {Registers.Graphics[graphicsIndex]}");
                        }
                        else
                        {
                            Console.WriteLine($"[CPU] SUBI: Invalid register index {regIndex}");
                        }
                        break;
                    }

                    case PromethiumOpcode.MULI:
                    {
                        // Format: [opcode][register][immediate (4 bytes)]
                        byte regIndex = FetchByte();
                        int immValue = FetchInt();
                        if (regIndex < Registers.GPR.Length)
                        {
                            int before = Registers.GPR[regIndex].AsInt;
                            Registers.GPR[regIndex].AsInt *= immValue;
                            Console.WriteLine($"[CPU] MULI: R{regIndex} ({before}) * {immValue} = {Registers.GPR[regIndex]}");
                            if (Registers.GPR[regIndex].AsInt == 0)
                                Registers.CpuFlag |= CpuFlags.Zero;
                            else
                                Registers.CpuFlag &= ~CpuFlags.Zero;
                        }
                        else if (regIndex < Registers.GPR.Length + Registers.Graphics.Length)
                        {
                            int graphicsIndex = regIndex - Registers.GPR.Length;
                            int before = Registers.Graphics[graphicsIndex].AsInt;
                            Registers.Graphics[graphicsIndex].AsInt *= immValue;
                            Console.WriteLine($"[CPU] MULI: G{graphicsIndex} ({before}) * {immValue} = {Registers.Graphics[graphicsIndex]}");
                        }
                        else
                        {
                            Console.WriteLine($"[CPU] MULI: Invalid register index {regIndex}");
                        }
                        break;
                    }
                case PromethiumOpcode.DIVI:
                    {
                        // Format: [opcode][register][immediate (4 bytes)]
                        byte regIndex = FetchByte();
                        int immValue = FetchInt();
                        if (regIndex < Registers.GPR.Length)
                        {
                            if (immValue != 0)
                            {
                                int before = Registers.GPR[regIndex].AsInt;
                                Registers.GPR[regIndex].AsInt /= immValue;
                                Console.WriteLine($"[CPU] DIVI: R{regIndex} ({before}) / {immValue} = {Registers.GPR[regIndex]}");
                                if (Registers.GPR[regIndex].AsInt == 0)
                                    Registers.CpuFlag |= CpuFlags.Zero;
                                else
                                    Registers.CpuFlag &= ~CpuFlags.Zero;
                            }
                            else
                            {
                                Console.WriteLine("[CPU] DIVI: Division by zero error.");
                            }
                        }
                        else if (regIndex < Registers.GPR.Length + Registers.Graphics.Length)
                        {
                            int graphicsIndex = regIndex - Registers.GPR.Length;
                            int before = Registers.Graphics[graphicsIndex].AsInt;
                            Registers.Graphics[graphicsIndex].AsInt /= immValue;
                            Console.WriteLine($"[CPU] DIVI: G{graphicsIndex} ({before}) / {immValue} = {Registers.Graphics[graphicsIndex]}");
                        }
                        else
                        {
                            Console.WriteLine($"[CPU] DIVI: Invalid register index {regIndex}");
                        }
                        break;
                    }
                    case PromethiumOpcode.ANDI:
                    {
                        // Format: [opcode][register][immediate (4 bytes)]
                        byte regIndex = FetchByte();
                        int immValue = FetchInt();
                        if (regIndex < Registers.GPR.Length)
                        {
                            int before = Registers.GPR[regIndex].AsInt;
                            Registers.GPR[regIndex].AsInt &= immValue;
                            Console.WriteLine($"[CPU] ANDI: R{regIndex} ({before}) & {immValue} = {Registers.GPR[regIndex]}");
                            if (Registers.GPR[regIndex].AsInt == 0)
                                Registers.CpuFlag |= CpuFlags.Zero;
                            else
                                Registers.CpuFlag &= ~CpuFlags.Zero;
                        }
                        else if (regIndex < Registers.GPR.Length + Registers.Graphics.Length)
                        {
                            int graphicsIndex = regIndex - Registers.GPR.Length;
                            int before = Registers.Graphics[graphicsIndex].AsInt;
                            Registers.Graphics[graphicsIndex].AsInt &= immValue;
                            Console.WriteLine($"[CPU] ANDI: G{graphicsIndex} ({before}) & {immValue} = {Registers.Graphics[graphicsIndex]}");
                        }
                        else
                        {
                            Console.WriteLine($"[CPU] ANDI: Invalid register index {regIndex}");
                        }
                        break;
                    }
                case PromethiumOpcode.ORI:
                    {
                        // Format: [opcode][register][immediate (4 bytes)]
                        byte regIndex = FetchByte();
                        int immValue = FetchInt();
                        if (regIndex < Registers.GPR.Length)
                        {
                            int before = Registers.GPR[regIndex].AsInt;
                            Registers.GPR[regIndex].AsInt |= immValue;
                            Console.WriteLine($"[CPU] ORI: R{regIndex} ({before}) | {immValue} = {Registers.GPR[regIndex]}");
                            if (Registers.GPR[regIndex].AsInt == 0)
                                Registers.CpuFlag |= CpuFlags.Zero;
                            else
                                Registers.CpuFlag &= ~CpuFlags.Zero;
                        }
                        else if (regIndex < Registers.GPR.Length + Registers.Graphics.Length)
                        {
                            int graphicsIndex = regIndex - Registers.GPR.Length;
                            int before = Registers.Graphics[graphicsIndex].AsInt;
                            Registers.Graphics[graphicsIndex].AsInt |= immValue;
                            Console.WriteLine($"[CPU] ORI: G{graphicsIndex} ({before}) | {immValue} = {Registers.Graphics[graphicsIndex]}");
                        }
                        else
                        {
                            Console.WriteLine($"[CPU] ORI: Invalid register index {regIndex}");
                        }
                        break;
                    }
                case PromethiumOpcode.XORI:
                    {
                        // Format: [opcode][register][immediate (4 bytes)]
                        byte regIndex = FetchByte();
                        int immValue = FetchInt();
                        if (regIndex < Registers.GPR.Length)
                        {
                            int before = Registers.GPR[regIndex].AsInt;
                            Registers.GPR[regIndex].AsInt ^= immValue;
                            Console.WriteLine($"[CPU] XORI: R{regIndex} ({before}) ^ {immValue} = {Registers.GPR[regIndex]}");
                            if (Registers.GPR[regIndex].AsInt == 0)
                                Registers.CpuFlag |= CpuFlags.Zero;
                            else
                                Registers.CpuFlag &= ~CpuFlags.Zero;
                        }
                        else if (regIndex < Registers.GPR.Length + Registers.Graphics.Length)
                        {
                            int graphicsIndex = regIndex - Registers.GPR.Length;
                            int before = Registers.Graphics[graphicsIndex].AsInt;
                            Registers.Graphics[graphicsIndex].AsInt ^= immValue;
                            Console.WriteLine($"[CPU] XORI: G{graphicsIndex} ({before}) ^ {immValue} = {Registers.Graphics[graphicsIndex]}");
                        }
                        else
                        {
                            Console.WriteLine($"[CPU] XORI: Invalid register index {regIndex}");
                        }
                        break;
                    }


                case PromethiumOpcode.CMPI:
                    {
                        // Format: [opcode][register][immediate (4 bytes)]
                        byte regIndex = FetchByte();
                        int immValue = FetchInt();
                        if (regIndex < Registers.GPR.Length)
                        {
                            int diff = Registers.GPR[regIndex].AsInt - immValue;

                            // Clear all previous comparison flags.
                            Registers.CpuFlag &= ~(CpuFlags.Zero | CpuFlags.Greater | CpuFlags.Less | CpuFlags.GreaterOrEqual | CpuFlags.LessOrEqual);

                            if (diff == 0)
                            {
                                Registers.CpuFlag |= CpuFlags.Zero | CpuFlags.GreaterOrEqual | CpuFlags.LessOrEqual;
                                Console.WriteLine($"[CPU] CMPI: R{regIndex} equals {immValue}");
                            }
                            else if (diff > 0)
                            {
                                Registers.CpuFlag |= CpuFlags.Greater | CpuFlags.GreaterOrEqual;
                                Console.WriteLine($"[CPU] CMPI: R{regIndex} is greater than {immValue}");
                            }
                            else  // diff < 0
                            {
                                Registers.CpuFlag |= CpuFlags.Less | CpuFlags.LessOrEqual;
                                Console.WriteLine($"[CPU] CMPI: R{regIndex} is less than {immValue}");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"[CPU] CMPI: Invalid register index {regIndex}");
                        }
                        break;
                    }

                case PromethiumOpcode.CMP:
                    {
                        // Format: [opcode][register1][register2]
                        byte regIndex1 = FetchByte();
                        byte regIndex2 = FetchByte();
                        if (regIndex1 < Registers.GPR.Length && regIndex2 < Registers.GPR.Length)
                        {
                            int val1 = Registers.GPR[regIndex1].AsInt;
                            int val2 = Registers.GPR[regIndex2].AsInt;
                            int diff = val1 - val2;

                            // Clear previous comparison flags.
                            Registers.CpuFlag &= ~(CpuFlags.Zero | CpuFlags.Greater | CpuFlags.Less);

                            if (diff == 0)
                            {
                                // When equal, set only the Zero flag.
                                Registers.CpuFlag |= CpuFlags.Zero;
                                Console.WriteLine($"[CPU] CMP: R{regIndex1} equals R{regIndex2}");
                            }
                            else if (diff > 0)
                            {
                                // When greater, set the Greater flag.
                                Registers.CpuFlag |= CpuFlags.Greater;
                                Console.WriteLine($"[CPU] CMP: R{regIndex1} is greater than R{regIndex2}");
                            }
                            else // diff < 0
                            {
                                // When less, set the Less flag.
                                Registers.CpuFlag |= CpuFlags.Less;
                                Console.WriteLine($"[CPU] CMP: R{regIndex1} is less than R{regIndex2}");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"[CPU] CMP: Invalid register index {regIndex1} or {regIndex2}");
                        }
                        break;
                    }




                // Unconditional Jump
                case PromethiumOpcode.JMP:
                    {
                        int address = FetchInt();
                        if (address >= 0 && address < Memory.ProgramSize)
                        {
                            Console.WriteLine($"[CPU] JMP: Jumping to address {address}");
                            PC = address;
                        }
                        else
                        {
                            Console.WriteLine($"[CPU] JMP: Invalid address {address}. Halting execution.");
                            Running = false;
                        }
                        break;
                    }

                // Jump if Zero (or Equal)
                case PromethiumOpcode.JZ:
                case PromethiumOpcode.JE:
                    {
                        int address = FetchInt();
                        if ((Registers.CpuFlag & CpuFlags.Zero) != 0)
                        {
                            Console.WriteLine($"[CPU] JZ/JE: Jumping to address {address}");
                            PC = address;
                        }
                        else
                        {
                            Console.WriteLine($"[CPU] JZ/JE: Not jumping to address {address} (zero flag not set).");
                        }
                        break;
                    }

                // Jump if Not Zero (Not Equal)
                case PromethiumOpcode.JNZ:
                case PromethiumOpcode.JNE:
                    {
                        int address = FetchInt();
                        if ((Registers.CpuFlag & CpuFlags.Zero) == 0)
                        {
                            Console.WriteLine($"[CPU] JNZ/JNE: Jumping to address {address}");
                            PC = address;
                        }
                        else
                        {
                            Console.WriteLine($"[CPU] JNZ/JNE: Not jumping to address {address} (zero flag set).");
                        }
                        break;
                    }

                // Jump if Greater (strictly)
                case PromethiumOpcode.JG:
                    {
                        int address = FetchInt();
                        // Must be strictly greater (Greater flag set, and not equal).
                        if ((Registers.CpuFlag & CpuFlags.Greater) != 0)
                        {
                            Console.WriteLine($"[CPU] JG: Jumping to address {address}");
                            PC = address;
                        }
                        else
                        {
                            Console.WriteLine($"[CPU] JG: Not jumping to address {address} (greater flag not set).");
                        }
                        break;
                    }

                // Jump if Less (strictly)
                case PromethiumOpcode.JL:
                    {
                        int address = FetchInt();
                        if ((Registers.CpuFlag & CpuFlags.Less) != 0)
                        {
                            Console.WriteLine($"[CPU] JL: Jumping to address {address}");
                            PC = address;
                        }
                        else
                        {
                            Console.WriteLine($"[CPU] JL: Not jumping to address {address} (less flag not set).");
                        }
                        break;
                    }

                // Jump if Greater or Equal (either Greater or Equal)
                case PromethiumOpcode.JGE:
                    {
                        int address = FetchInt();
                        if (((Registers.CpuFlag & CpuFlags.Greater) != 0) || ((Registers.CpuFlag & CpuFlags.Zero) != 0))
                        {
                            Console.WriteLine($"[CPU] JGE: Jumping to address {address}");
                            PC = address;
                        }
                        else
                        {
                            Console.WriteLine($"[CPU] JGE: Not jumping to address {address} (condition not met).");
                        }
                        break;
                    }

                // Jump if Less or Equal (either Less or Equal)
                case PromethiumOpcode.JLE:
                    {
                        int address = FetchInt();
                        if (((Registers.CpuFlag & CpuFlags.Less) != 0) || ((Registers.CpuFlag & CpuFlags.Zero) != 0))
                        {
                            Console.WriteLine($"[CPU] JLE: Jumping to address {address}");
                            PC = address;
                        }
                        else
                        {
                            Console.WriteLine($"[CPU] JLE: Not jumping to address {address} (condition not met).");
                        }
                        break;
                    }


                case PromethiumOpcode.CALL:
                    {
                        // Format: [opcode][address (4 bytes)]
                        int address = FetchInt();
                        if (address >= 0 && address < Memory.ProgramSize)
                        {
                            Console.WriteLine($"[CPU] CALL: Calling subroutine at address {address}");
                            callStack.Add(PC); // Save current PC to stack.
                            PC = address; // Jump to subroutine.
                        }
                        else
                        {
                            Console.WriteLine($"[CPU] CALL: Invalid address {address}. Halting execution.");
                            Running = false;
                        }
                        break;
                    }
                case PromethiumOpcode.RET:
                    {
                        // Format: [opcode]
                        if (callStack.Count > 0)
                        {
                            PC = callStack[^1]; // Get the last saved PC from the stack.
                            callStack.RemoveAt(callStack.Count - 1); // Remove it from the stack.
                            Console.WriteLine($"[CPU] RET: Returning to address {PC}");
                        }
                        else
                        {
                            Console.WriteLine("[CPU] RET: Call stack is empty. Halting execution.");
                            Running = false;
                        }
                        break;
                    }
                case PromethiumOpcode.PUSH:
                    {
                        // Format: [opcode][register]
                        byte regIndex = FetchByte();
                        if (regIndex < Registers.GPR.Length)
                        {
                            int value = Registers.GPR[regIndex].AsInt;
                            Memory.Push(value);
                            Console.WriteLine($"[CPU] PUSH: Pushed R{regIndex} value {value} to stack.");
                        }
                        else
                        {
                            Console.WriteLine($"[CPU] PUSH: Invalid register index {regIndex}");
                        }
                        break;
                    }
                case PromethiumOpcode.POP:
                    {
                        // Format: [opcode][register]
                        byte regIndex = FetchByte();
                        if (regIndex < Registers.GPR.Length)
                        {
                            int value = Memory.Pop();
                            Registers.GPR[regIndex].AsInt = value;
                            Console.WriteLine($"[CPU] POP: Popped value {value} from stack to R{regIndex}.");
                        }
                        else
                        {
                            Console.WriteLine($"[CPU] POP: Invalid register index {regIndex}");
                        }
                        break;
                    }

                case PromethiumOpcode.IN:
                    {
                        // Format: [opcode][port]
                        byte port = FetchByte();
                        int value = Memory.Read(MemoryDomain.IO, port);
                        Console.WriteLine($"[CPU] IN: Read value {value} from port {port}");
                        break;
                    }
                case PromethiumOpcode.OUT:
                    {
                        // Format: [opcode][port][value]
                        byte port = FetchByte();
                        int value = FetchInt();
                        Memory.Write(MemoryDomain.IO, port, (byte)value);
                        Console.WriteLine($"[CPU] OUT: Wrote value {value} to port {port}");
                        break;
                    }
                case PromethiumOpcode.RAND:
                    {
                        // Format: [opcode][register]
                        byte regIndex = FetchByte();
                        if (regIndex < Registers.GPR.Length)
                        {
                            Random rand = new Random();
                            int randomValue = rand.Next(int.MinValue, int.MaxValue);
                            Registers.GPR[regIndex].AsInt = randomValue;
                            Console.WriteLine($"[CPU] RAND: Generated random value {randomValue} into R{regIndex}");
                        }
                        else
                        {
                            Console.WriteLine($"[CPU] RAND: Invalid register index {regIndex}");
                        }
                        break;
                    }
                case PromethiumOpcode.TIME:
                    {
                        // Format: [opcode][register]
                        byte regIndex = FetchByte();
                        if (regIndex < Registers.GPR.Length)
                        {
                            int currentTime = Environment.TickCount; // Get system tick count.
                            Registers.GPR[regIndex].AsFloat = currentTime;
                            Console.WriteLine($"[CPU] TIME: Current time {currentTime} into R{regIndex}");
                        }
                        else
                        {
                            Console.WriteLine($"[CPU] TIME: Invalid register index {regIndex}");
                        }
                        break;
                    }
                case PromethiumOpcode.INT:
                    {
                        // Format: [opcode][interrupt type]
                        byte interruptType = FetchByte();
                        Console.WriteLine($"[CPU] INT: Interrupt request of type {interruptType} received.");
                        //(not implemented).
                        break;
                    }
                case PromethiumOpcode.IRET:
                    {
                        // Format: [opcode]
                        Console.WriteLine("[CPU] IRET: Returning from interrupt (not implemented).");
                        break;
                    }



                case PromethiumOpcode.HLT:
                    {
                        Console.WriteLine("[CPU] HLT encountered. Halting execution.");
                        Running = false;
                        break;
                    }
                case PromethiumOpcode.MOVF:
                    {
                        // Format: [opcode][float value (4 bytes)][register]
                        float floatValue = FetchFloat();
                        byte regIndex = FetchByte();

                        if (regIndex < Registers.GPR.Length)
                        {
                            Registers.GPR[regIndex].AsFloat = floatValue;
                            Registers.GPRType[regIndex] = RegisterType.Float;
                            Console.WriteLine($"[CPU] MOVF: Moved float value {floatValue} into R{regIndex}");
                        }
                        else
                        {
                            Console.WriteLine($"[CPU] MOVF: Invalid register index {regIndex}");
                        }
                        break;
                    }
                case PromethiumOpcode.ITOF: //Converts a register whos type is a int into a float
                    {
                        // Format: [opcode][register]
                        byte regIndex = FetchByte();
                        if (regIndex < Registers.GPR.Length)
                        {
                            int intValue = Registers.GPR[regIndex].AsInt;
                            float floatValue = (float)intValue;
                            Registers.GPR[regIndex].AsFloat = floatValue;
                            Registers.GPRType[regIndex] = RegisterType.Float;
                            Console.WriteLine($"[CPU] ITOF: Converted int {intValue} from R{regIndex} to float {floatValue}");
                        }
                        else
                        {
                            Console.WriteLine($"[CPU] ITOF: Invalid register index {regIndex}");
                        }
                        break;
                    }

                case PromethiumOpcode.FTOI: //Converts a register whos type is a float into a int
                    {
                        // Format: [opcode][register]
                        byte regIndex = FetchByte();
                        if (regIndex < Registers.GPR.Length)
                        {
                            float floatValue = Registers.GPR[regIndex].AsFloat;
                            int intValue = (int)floatValue;
                            Registers.GPR[regIndex].AsInt = intValue;
                            Registers.GPRType[regIndex] = RegisterType.Integer;
                            Console.WriteLine($"[CPU] FTOI: Converted float {floatValue} from R{regIndex} to int {intValue}");
                        }
                        else
                        {
                            Console.WriteLine($"[CPU] FTOI: Invalid register index {regIndex}");
                        }
                        break;
                    }

                case PromethiumOpcode.FADD: // add a float to a register FADD 1.2 R3
                    {
                        // Format: [opcode][float value (4 bytes)][register]
                        float floatValue = FetchFloat();
                        byte regIndex = FetchByte();

                        if (regIndex < Registers.GPR.Length)
                        {
                            float before = Registers.GPR[regIndex].AsFloat;
                            Registers.GPR[regIndex].AsFloat += floatValue;
                            Registers.GPRType[regIndex] = RegisterType.Float;
                            Console.WriteLine($"[CPU] FADD: Added float {floatValue} to R{regIndex} ({before}) = {Registers.GPR[regIndex].AsFloat}");

                            if (Registers.GPR[regIndex].AsFloat == 0)
                                Registers.CpuFlag |= CpuFlags.Zero;
                            else
                                Registers.CpuFlag &= ~CpuFlags.Zero;
                        }
                        else
                        {
                            Console.WriteLine($"[CPU] FADD: Invalid register index {regIndex}");
                        }
                        break;
                    }
                case PromethiumOpcode.FSUB: // subtract a float from a register FSUB 1.2 R3
                    {
                        // Format: [opcode][float value (4 bytes)][register]
                        float floatValue = FetchFloat();
                        byte regIndex = FetchByte();
                        if (regIndex < Registers.GPR.Length)
                        {
                            float before = Registers.GPR[regIndex].AsFloat;
                            Registers.GPR[regIndex].AsFloat -= floatValue;
                            Registers.GPRType[regIndex] = RegisterType.Float;
                            Console.WriteLine($"[CPU] FSUB: Subtracted float {floatValue} from R{regIndex} ({before}) = {Registers.GPR[regIndex].AsFloat}");
                            if (Registers.GPR[regIndex].AsFloat == 0)
                                Registers.CpuFlag |= CpuFlags.Zero;
                            else
                                Registers.CpuFlag &= ~CpuFlags.Zero;
                        }
                        else
                        {
                            Console.WriteLine($"[CPU] FSUB: Invalid register index {regIndex}");
                        }
                        break;
                    }
                case PromethiumOpcode.FMUL: // multiply a float with a register FMUL 1.2 R3
                    {
                        // Format: [opcode][float value (4 bytes)][register]
                        float floatValue = FetchFloat();
                        byte regIndex = FetchByte();
                        if (regIndex < Registers.GPR.Length)
                        {
                            float before = Registers.GPR[regIndex].AsFloat;
                            Registers.GPR[regIndex].AsFloat *= floatValue;
                            Registers.GPRType[regIndex] = RegisterType.Float;
                            Console.WriteLine($"[CPU] FMUL: Multiplied R{regIndex} ({before}) * {floatValue} = {Registers.GPR[regIndex].AsFloat}");
                            if (Registers.GPR[regIndex].AsFloat == 0)
                                Registers.CpuFlag |= CpuFlags.Zero;
                            else
                                Registers.CpuFlag &= ~CpuFlags.Zero;
                        }
                        else
                        {
                            Console.WriteLine($"[CPU] FMUL: Invalid register index {regIndex}");
                        }
                        break;
                    }
                case PromethiumOpcode.FDIV: // divide a float by a register FDIV 1.2 R3
                    {
                        // Format: [opcode][float value (4 bytes)][register]
                        float floatValue = FetchFloat();
                        byte regIndex = FetchByte();
                        if (regIndex < Registers.GPR.Length)
                        {
                            if (floatValue != 0)
                            {
                                float before = Registers.GPR[regIndex].AsFloat;
                                Registers.GPR[regIndex].AsFloat /= floatValue;
                                Registers.GPRType[regIndex] = RegisterType.Float;
                                Console.WriteLine($"[CPU] FDIV: Divided R{regIndex} ({before}) / {floatValue} = {Registers.GPR[regIndex].AsFloat}");
                                if (Registers.GPR[regIndex].AsFloat == 0)
                                    Registers.CpuFlag |= CpuFlags.Zero;
                                else
                                    Registers.CpuFlag &= ~CpuFlags.Zero;
                            }
                            else
                            {
                                Console.WriteLine("[CPU] FDIV: Division by zero error.");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"[CPU] FDIV: Invalid register index {regIndex}");
                        }
                        break;
                    }
                case PromethiumOpcode.FMOD: // modulo a float by a register FMOD 1.2 R3
                    {
                        // Format: [opcode][float value (4 bytes)][register]
                        float floatValue = FetchFloat();
                        byte regIndex = FetchByte();
                        if (regIndex < Registers.GPR.Length)
                        {
                            if (floatValue != 0)
                            {
                                float before = Registers.GPR[regIndex].AsFloat;
                                Registers.GPR[regIndex].AsFloat %= floatValue;
                                Registers.GPRType[regIndex] = RegisterType.Float;
                                Console.WriteLine($"[CPU] FMOD: Modulo R{regIndex} ({before}) % {floatValue} = {Registers.GPR[regIndex].AsFloat}");
                                if (Registers.GPR[regIndex].AsFloat == 0)
                                    Registers.CpuFlag |= CpuFlags.Zero;
                                else
                                    Registers.CpuFlag &= ~CpuFlags.Zero;
                            }
                            else
                            {
                                Console.WriteLine("[CPU] FMOD: Division by zero error.");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"[CPU] FMOD: Invalid register index {regIndex}");
                        }
                        break;
                    }

                case PromethiumOpcode.DLSTART:
                    {
                        byte modelNameLength = FetchByte(); // Fetch the length of the model name.
                        if (modelNameLength > 0)
                        {
                            byte[] modelNameBytes = new byte[modelNameLength];
                            for (int i = 0; i < modelNameLength; i++)
                            {
                                modelNameBytes[i] = FetchByte(); // Fetch each byte of the model name.
                            }
                            string modelName = Encoding.ASCII.GetString(modelNameBytes);
                            Console.WriteLine($"[CPU] DLSTART: Starting display list for model '{modelName}'");
                            _displayListManager.StartDisplayList(modelName); // Use the instance
                        }
                        else
                        {
                            Console.WriteLine("[CPU] DLSTART: Invalid model name length (0).");
                        }
                        break;
                    }

                case PromethiumOpcode.DLPRIMITIVE:
                    {
                        byte primitiveType = FetchByte();
                        if (Enum.IsDefined(typeof(PrimitiveType), primitiveType))
                        {
                            Console.WriteLine($"[CPU] DLPRIMITIVE: Setting primitive type to {(PrimitiveType)primitiveType}");
                            _displayListManager.AddPrimitive((PrimitiveType)primitiveType); // Use the instance
                        }
                        else
                        {
                            Console.WriteLine($"[CPU] DLPRIMITIVE: Invalid primitive type {primitiveType}");
                        }
                        break;
                    }

                case PromethiumOpcode.DLCOLOR:
                    {
                        byte r = FetchByte();
                        byte g = FetchByte();
                        byte b = FetchByte();
                        string colorHex = $"{r:X2}{g:X2}{b:X2}";
                        Console.WriteLine($"[CPU] DLCOLOR: Setting color to #{colorHex}");
                        _displayListManager.AddColor(colorHex); // Use the instance
                        break;
                    }

                case PromethiumOpcode.DLVERTEX:
                    {
                        float x = FetchFloat();
                        float y = FetchFloat();
                        float z = FetchFloat();
                        Console.WriteLine($"[CPU] DLVERTEX: Adding vertex ({x}, {y}, {z})");
                        _displayListManager.AddVertex(x, y, z); // Use the instance
                        break;
                    }

                case PromethiumOpcode.DLEND:
                    {
                        Console.WriteLine("[CPU] DLEND: Ending the current display list.");
                        _displayListManager.EndDisplayList(); // Use the instance
                        break;
                    }

                case PromethiumOpcode.DLCALL:
                    {
                        byte modelNameLength = FetchByte(); // Fetch the length of the model name
                        if (modelNameLength > 0)
                        {
                            byte[] modelNameBytes = new byte[modelNameLength];
                            for (int i = 0; i < modelNameLength; i++)
                            {
                                modelNameBytes[i] = FetchByte(); // Fetch each byte of the model name
                            }
                            string modelName = Encoding.ASCII.GetString(modelNameBytes);

                            // Fetch the optional coordinates (default to 0, 0, 0 if not provided)
                            float x = FetchFloat();
                            float y = FetchFloat();
                            float z = FetchFloat();

                            Console.WriteLine($"[CPU] DLCALL: Loading display list '{modelName}' at position ({x}, {y}, {z})");

                            try
                            {
                                // Ensure the display list is stored in the DPL memory domain
                                if (!_displayListManager.SerializeDisplayList(modelName).Any())
                                {
                                    _displayListManager.StoreDisplayListToDpl(modelName, Memory, _nextDplAddress); // Store at the next available address
                                    byte[] serializedData = _displayListManager.SerializeDisplayList(modelName);
                                    _nextDplAddress += serializedData.Length; // Update the pointer for the next model
                                }

                                // Optionally copy the serialized display list to the Video memory domain for rendering
                                byte[] serializedDataForVideo = _displayListManager.SerializeDisplayList(modelName);
                                for (int i = 0; i < serializedDataForVideo.Length; i++)
                                {
                                    Memory.Write(MemoryDomain.Video, i, serializedDataForVideo[i]);
                                }
                                Console.WriteLine($"[CPU] DLCALL: Display list '{modelName}' copied to Video memory for rendering.");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"[CPU] DLCALL: Error loading display list '{modelName}': {ex.Message}");
                            }
                        }
                        else
                        {
                            Console.WriteLine("[CPU] DLCALL: Invalid model name length (0).");
                        }
                        break;
                    }



                case PromethiumOpcode.STOREMODEL:
                    {
                        byte regIndex = FetchByte(); // Graphics register index
                        regIndex -= PasmLoader.GeneralRegisterCount; // Adjust for graphics register offset
                        byte modelNameLength = FetchByte(); // Length of the model name

                        if (modelNameLength > 0 && regIndex < Registers.Graphics.Length)
                        {
                            byte[] modelNameBytes = new byte[modelNameLength];
                            for (int i = 0; i < modelNameLength; i++)
                            {
                                modelNameBytes[i] = FetchByte();
                            }
                            string modelName = Encoding.ASCII.GetString(modelNameBytes);

                            // Serialize and store the display list in the DPL memory domain
                            try
                            {
                                _displayListManager.StoreDisplayListToDpl(modelName, Memory, _nextDplAddress);
                                byte[] serializedData = _displayListManager.SerializeDisplayListAs3D(modelName);
                                _nextDplAddress += serializedData.Length;


                                Registers.Graphics[regIndex].AsModel = modelName; // Store the model name in the register
                                Registers.GraphicsType[regIndex] = RegisterType.Model; // Set the type to Model
                                Console.WriteLine($"[CPU] STOREMODEL: Stored model '{modelName}' in G{regIndex} and DPL memory.");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"[CPU] STOREMODEL: Error storing model '{modelName}': {ex.Message}");
                            }
                        }
                        else
                        {
                            Console.WriteLine("[CPU] STOREMODEL: Invalid model name length or register index.");
                        }
                        break;
                    }




                case PromethiumOpcode.LOADMODEL:
                    {
                        byte regIndex = FetchByte(); // Graphics register index
                        regIndex -= PasmLoader.GeneralRegisterCount; // Adjust for graphics register offset

                        if (regIndex < Registers.Graphics.Length)
                        {
                            if (Registers.GraphicsType[regIndex] == RegisterType.Model)
                            {
                                string modelName = Registers.Graphics[regIndex].AsModel;
                                if (!string.IsNullOrEmpty(modelName))
                                {
                                    float x = FetchFloat();
                                    float y = FetchFloat();
                                    float z = FetchFloat();

                                    Console.WriteLine($"[CPU] LOADMODEL: Loading model '{modelName}' at position ({x}, {y}, {z})");

                                    try
                                    {
                                        // Ensure the model exists in the DPL memory domain
                                        byte[] serializedDataForVideo = _displayListManager.SerializeDisplayListAs3D(modelName);
                                        for (int i = 0; i < serializedDataForVideo.Length; i++)
                                        {
                                            Memory.Write(MemoryDomain.Video, i, serializedDataForVideo[i]);
                                        }
                                        {
                                            Console.WriteLine($"[CPU] LOADMODEL: Model '{modelName}' not found in DPL memory. Attempting to store it.");
                                            _displayListManager.StoreDisplayListToDpl(modelName, Memory, _nextDplAddress);
                                            byte[] serializedData = _displayListManager.SerializeDisplayList(modelName);
                                            _nextDplAddress += serializedData.Length; // Update the pointer for the next model
                                        }


                                        // Render the model using the display list manager
                                        _displayListManager.CallDisplayList(modelName, x, y, z);
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"[CPU] LOADMODEL: Error loading model '{modelName}': {ex.Message}");
                                    }
                                }
                                else
                                {
                                    Console.WriteLine($"[CPU] LOADMODEL: No model stored in G{regIndex}");
                                }
                            }
                            else
                            {
                                Console.WriteLine($"[CPU] LOADMODEL: G{regIndex} does not contain a model.");
                            }
                        }
                        else
                        {
                            Console.WriteLine("[CPU] LOADMODEL: Invalid graphics register index.");
                        }
                        break;
                    }










                default:
                    {
                        Console.WriteLine($"[CPU] Unknown opcode {opcode}. Halting execution.");
                        Running = false;
                        break;
                    }
            }
        }
    }
}
