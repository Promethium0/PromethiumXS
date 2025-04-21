using System;
using System.Collections.Generic;
using System.Numerics;
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











        public Cpu(Memory memory, PromethiumRegisters registers)
        {
            Memory = memory;
            Registers = registers;



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
            byte value = Memory.Read(MemoryDomain.SystemCode, PC);
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
            int value = Memory.Read(MemoryDomain.SystemCode, PC)
                      | Memory.Read(MemoryDomain.SystemCode, PC + 1) << 8
                      | Memory.Read(MemoryDomain.SystemCode, PC + 2) << 16
                      | Memory.Read(MemoryDomain.SystemCode, PC + 3) << 24;
            PC += 4;
            return value;
        }

        private float FetchFloat()
        {
            // Read 4 bytes from memory and convert to float
            byte[] bytes = new byte[4];
            for (int i = 0; i < 4; i++)
            {
                bytes[i] = (byte)Memory.Read(MemoryDomain.SystemCode, PC + i);
            }
            PC += 4;
            // In your FetchFloat or similar method



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
                        byte destReg = FetchByte();
                        int immediate = FetchInt();

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
                        int value = Memory.Read(MemoryDomain.IORegisters, port);
                        Console.WriteLine($"[CPU] IN: Read value {value} from port {port}");
                        break;
                    }
                case PromethiumOpcode.OUT:
                    {
                        // Format: [opcode][port][value]
                        byte port = FetchByte();
                        int value = FetchInt();
                        Memory.Write(MemoryDomain.IORegisters, port, (byte)value);
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


                case PromethiumOpcode.STOREI:
                    {
                        byte domainByte = FetchByte();
                        byte offsetType = FetchByte(); // New: Indicates if the offset is immediate or a register
                        int offset = 0;

                        if (offsetType == 0) // Immediate offset
                        {
                            offset = FetchInt();
                        }
                        else if (offsetType == 1) // Register offset
                        {
                            byte offsetReg = FetchByte();
                            if (offsetReg < Registers.GPR.Length)
                            {
                                offset = Registers.GPR[offsetReg].AsInt;
                            }
                            else
                            {
                                Console.WriteLine($"[CPU] STOREI: Invalid register index for offset {offsetReg}");
                                break;
                            }
                        }
                        else
                        {
                            Console.WriteLine($"[CPU] STOREI: Invalid offset type {offsetType}");
                            break;
                        }

                        byte regIndex = FetchByte();

                        if (!Enum.IsDefined(typeof(MemoryDomain), (int)domainByte))
                        {
                            Console.WriteLine($"[CPU] STOREI: Invalid memory domain {domainByte}");
                            break;
                        }

                        MemoryDomain domain = (MemoryDomain)domainByte;

                        if (regIndex < Registers.GPR.Length)
                        {
                            try
                            {
                                Memory.WriteInt(domain, offset, Registers.GPR[regIndex].AsInt);
                                Console.WriteLine($"[CPU] STOREI: Stored value {Registers.GPR[regIndex].AsInt} from R{regIndex} to {domain}:{offset:X8}");
                            }
                            catch (ArgumentOutOfRangeException)
                            {
                                Console.WriteLine($"[CPU] STOREI: Memory access out of range - {domain}:{offset:X8}");
                                Registers.CpuFlag |= CpuFlags.Error;
                            }
                        }
                        else
                        {
                            Console.WriteLine($"[CPU] STOREI: Invalid register index {regIndex}");
                        }
                        break;
                    }


                case PromethiumOpcode.LOADI:
                    {
                        byte domainByte = FetchByte();
                        int offset = FetchInt();
                        byte regIndex = FetchByte();

                        if (!Enum.IsDefined(typeof(MemoryDomain), (int)domainByte))
                        {
                            Console.WriteLine($"[CPU] LOADI: Invalid memory domain {domainByte}");
                            break;
                        }

                        MemoryDomain domain = (MemoryDomain)domainByte;

                        // Handle general-purpose registers
                        if (regIndex < Registers.GPR.Length)
                        {
                            try
                            {
                                int value = Memory.ReadInt(domain, offset);
                                Registers.GPR[regIndex].AsInt = value;
                                Console.WriteLine($"[CPU] LOADI: Loaded value {value} into R{regIndex} from {domain}:{offset:X8}");
                            }
                            catch (ArgumentOutOfRangeException)
                            {
                                Console.WriteLine($"[CPU] LOADI: Memory access out of range - {domain}:{offset:X8}");
                                Registers.CpuFlag |= CpuFlags.Error;
                            }
                        }
                        else
                        {
                            Console.WriteLine($"[CPU] LOADI: Invalid register index {regIndex}");
                        }
                        break;
                    }


                case PromethiumOpcode.LI:
                    {
                        // Format: [opcode][immediate (4 bytes)][register (1 byte)]
                        int immediate = FetchInt(); // Fetch the immediate value first
                        byte regIndex = FetchByte(); // Fetch the register index

                        // Check if the register index is valid
                        if (regIndex < Registers.GPR.Length)
                        {
                            Registers.GPR[regIndex].AsInt = immediate; // Load the immediate value into the register
                            Console.WriteLine($"[CPU] LI: Loaded immediate value {immediate} into R{regIndex}");
                        }
                        else
                        {
                            Console.WriteLine($"[CPU] LI: Invalid register index {regIndex}");
                        }
                        break;
                    }







                case PromethiumOpcode.MAT_INIT:
                    {
                        byte matrixReg = FetchByte();
                        if (matrixReg < Registers.Matrices.Length)
                        {
                            float[] values = new float[6];
                            for (int i = 0; i < 6; i++)
                            {
                                values[i] = FetchFloat();
                            }
                            Matrix3x2 matrix3x2 = new Matrix3x2(values[0], values[1], values[2], values[3], values[4], values[5]);
                            Registers.Matrices[matrixReg] = new Matrix4x4(
                                matrix3x2.M11, matrix3x2.M12, 0, 0,
                                matrix3x2.M21, matrix3x2.M22, 0, 0,
                                0, 0, 1, 0,
                                matrix3x2.M31, matrix3x2.M32, 0, 1
                            );
                            Console.WriteLine($"[CPU] MAT_INIT: Initialized matrix M{matrixReg}");
                        }
                        else
                        {
                            Console.WriteLine($"[CPU] MAT_INIT: Invalid matrix register {matrixReg}");
                        }
                        break;
                    }



                case PromethiumOpcode.MAT_IDENTITY:
                    {
                        // Format: [opcode][matrix register]
                        byte matrixReg = FetchByte();
                        if (matrixReg < Registers.Matrices.Length)
                        {
                            Registers.Matrices[matrixReg] = Matrix4x4.Identity;
                            Console.WriteLine($"[CPU] MAT_IDENTITY: Set M{matrixReg} to identity matrix");
                        }
                        else
                        {
                            Console.WriteLine($"[CPU] MAT_IDENTITY: Invalid matrix register {matrixReg}");
                        }
                        break;
                    }

                case PromethiumOpcode.MAT_MUL:
                    {
                        // Format: [opcode][dest matrix][matrix1][matrix2]
                        byte destMatrix = FetchByte();
                        byte matrix1 = FetchByte();
                        byte matrix2 = FetchByte();
                        if (destMatrix < Registers.Matrices.Length &&
                            matrix1 < Registers.Matrices.Length &&
                            matrix2 < Registers.Matrices.Length)
                        {
                            Registers.Matrices[destMatrix] = Registers.Matrices[matrix1] * Registers.Matrices[matrix2];
                            Console.WriteLine($"[CPU] MAT_MUL: M{destMatrix} = M{matrix1} * M{matrix2}");
                        }
                        else
                        {
                            Console.WriteLine($"[CPU] MAT_MUL: Invalid matrix register(s)");
                        }
                        break;
                    }

                case PromethiumOpcode.MAT_TRANSPOSE:
                    {
                        // Format: [opcode][matrix register]
                        byte matrixReg = FetchByte();
                        if (matrixReg < Registers.Matrices.Length)
                        {
                            Registers.Matrices[matrixReg] = Matrix4x4.Transpose(Registers.Matrices[matrixReg]);
                            Console.WriteLine($"[CPU] MAT_TRANSPOSE: Transposed M{matrixReg}");
                        }
                        else
                        {
                            Console.WriteLine($"[CPU] MAT_TRANSPOSE: Invalid matrix register {matrixReg}");
                        }
                        break;
                    }

                case PromethiumOpcode.MAT_INVERSE:
                    {
                        // Format: [opcode][matrix register]
                        byte matrixReg = FetchByte();
                        if (matrixReg < Registers.Matrices.Length)
                        {
                            if (Matrix4x4.Invert(Registers.Matrices[matrixReg], out var invertedMatrix))
                            {
                                Registers.Matrices[matrixReg] = invertedMatrix;
                                Console.WriteLine($"[CPU] MAT_INVERSE: Inverted M{matrixReg}");
                            }
                            else
                            {
                                Console.WriteLine($"[CPU] MAT_INVERSE: M{matrixReg} is not invertible");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"[CPU] MAT_INVERSE: Invalid matrix register {matrixReg}");
                        }
                        break;
                    }

                case PromethiumOpcode.VEC_INIT:
                    {
                        // Format: [opcode][vector register][x][y][z]
                        byte vectorReg = FetchByte();
                        if (vectorReg < Registers.Vectors.Length)
                        {
                            float x = FetchFloat();
                            float y = FetchFloat();
                            float z = FetchFloat();
                            Registers.Vectors[vectorReg] = new Vector3(x, y, z);
                            Console.WriteLine($"[CPU] VEC_INIT: Initialized V{vectorReg} = ({x}, {y}, {z})");
                        }
                        else
                        {
                            Console.WriteLine($"[CPU] VEC_INIT: Invalid vector register {vectorReg}");
                        }
                        break;
                    }

                case PromethiumOpcode.VEC_ADD:
                    {
                        // Format: [opcode][dest vector][vector1][vector2]
                        byte destVector = FetchByte();
                        byte vector1 = FetchByte();
                        byte vector2 = FetchByte();
                        if (destVector < Registers.Vectors.Length &&
                            vector1 < Registers.Vectors.Length &&
                            vector2 < Registers.Vectors.Length)
                        {
                            Registers.Vectors[destVector] = Registers.Vectors[vector1] + Registers.Vectors[vector2];
                            Console.WriteLine($"[CPU] VEC_ADD: V{destVector} = V{vector1} + V{vector2}");
                        }
                        else
                        {
                            Console.WriteLine($"[CPU] VEC_ADD: Invalid vector register(s)");
                        }
                        break;
                    }

                case PromethiumOpcode.VEC_SUB:
                    {
                        // Format: [opcode][dest vector][vector1][vector2]
                        byte destVector = FetchByte();
                        byte vector1 = FetchByte();
                        byte vector2 = FetchByte();
                        if (destVector < Registers.Vectors.Length &&
                            vector1 < Registers.Vectors.Length &&
                            vector2 < Registers.Vectors.Length)
                        {
                            Registers.Vectors[destVector] = Registers.Vectors[vector1] - Registers.Vectors[vector2];
                            Console.WriteLine($"[CPU] VEC_SUB: V{destVector} = V{vector1} - V{vector2}");
                        }
                        else
                        {
                            Console.WriteLine($"[CPU] VEC_SUB: Invalid vector register(s)");
                        }
                        break;
                    }

                case PromethiumOpcode.VEC_MUL:
                    {
                        // Format: [opcode][dest vector][vector][scalar]
                        byte destVector = FetchByte();
                        byte vector = FetchByte();
                        float scalar = FetchFloat();
                        if (destVector < Registers.Vectors.Length && vector < Registers.Vectors.Length)
                        {
                            Registers.Vectors[destVector] = Registers.Vectors[vector] * scalar;
                            Console.WriteLine($"[CPU] VEC_MUL: V{destVector} = V{vector} * {scalar}");
                        }
                        else
                        {
                            Console.WriteLine($"[CPU] VEC_MUL: Invalid vector register(s)");
                        }
                        break;
                    }

                case PromethiumOpcode.VEC_DIV:
                    {
                        // Format: [opcode][dest vector][vector][scalar]
                        byte destVector = FetchByte();
                        byte vector = FetchByte();
                        float scalar = FetchFloat();
                        if (destVector < Registers.Vectors.Length && vector < Registers.Vectors.Length)
                        {
                            if (scalar != 0)
                            {
                                Registers.Vectors[destVector] = Registers.Vectors[vector] / scalar;
                                Console.WriteLine($"[CPU] VEC_DIV: V{destVector} = V{vector} / {scalar}");
                            }
                            else
                            {
                                Console.WriteLine($"[CPU] VEC_DIV: Division by zero error");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"[CPU] VEC_DIV: Invalid vector register(s)");
                        }
                        break;
                    }

                case PromethiumOpcode.VEC_DOT:
                    {
                        // Format: [opcode][dest register][vector1][vector2]
                        byte destReg = FetchByte();
                        byte vector1 = FetchByte();
                        byte vector2 = FetchByte();
                        if (destReg < Registers.GPR.Length &&
                            vector1 < Registers.Vectors.Length &&
                            vector2 < Registers.Vectors.Length)
                        {
                            float dotProduct = Vector3.Dot(Registers.Vectors[vector1], Registers.Vectors[vector2]);
                            Registers.GPR[destReg].AsFloat = dotProduct;
                            Console.WriteLine($"[CPU] VEC_DOT: R{destReg} = Dot(V{vector1}, V{vector2}) = {dotProduct}");
                        }
                        else
                        {
                            Console.WriteLine($"[CPU] VEC_DOT: Invalid register or vector register(s)");
                        }
                        break;
                    }

                case PromethiumOpcode.VEC_CROSS:
                    {
                        // Format: [opcode][dest vector][vector1][vector2]
                        byte destVector = FetchByte();
                        byte vector1 = FetchByte();
                        byte vector2 = FetchByte();
                        if (destVector < Registers.Vectors.Length &&
                            vector1 < Registers.Vectors.Length &&
                            vector2 < Registers.Vectors.Length)
                        {
                            Registers.Vectors[destVector] = Vector3.Cross(Registers.Vectors[vector1], Registers.Vectors[vector2]);
                            Console.WriteLine($"[CPU] VEC_CROSS: V{destVector} = Cross(V{vector1}, V{vector2})");
                        }
                        else
                        {
                            Console.WriteLine($"[CPU] VEC_CROSS: Invalid vector register(s)");
                        }
                        break;
                    }

                case PromethiumOpcode.VEC_LEN:
                    {
                        // Format: [opcode][dest register][vector]
                        byte destReg = FetchByte();
                        byte vector = FetchByte();
                        if (destReg < Registers.GPR.Length && vector < Registers.Vectors.Length)
                        {
                            float length = Registers.Vectors[vector].Length();
                            Registers.GPR[destReg].AsFloat = length;
                            Console.WriteLine($"[CPU] VEC_LEN: R{destReg} = Length(V{vector}) = {length}");
                        }
                        else
                        {
                            Console.WriteLine($"[CPU] VEC_LEN: Invalid register or vector register");
                        }
                        break;
                    }

                case PromethiumOpcode.VEC_NORM:
                    {
                        // Format: [opcode][dest vector][vector]
                        byte destVector = FetchByte();
                        byte vector = FetchByte();
                        if (destVector < Registers.Vectors.Length && vector < Registers.Vectors.Length)
                        {
                            Registers.Vectors[destVector] = Vector3.Normalize(Registers.Vectors[vector]);
                            Console.WriteLine($"[CPU] VEC_NORM: V{destVector} = Normalize(V{vector})");
                        }
                        else
                        {
                            Console.WriteLine($"[CPU] VEC_NORM: Invalid vector register(s)");
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
        


