using System;
using System.Collections.Generic;
using System.Threading;

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
                Registers.GPR[i] = 0;
            }
            // Reset graphics registers.
            for (int i = 0; i < Registers.Graphics.Length; i++)
            {
                Registers.Graphics[i] = 0;
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

        /// <summary>
        /// Executes instructions until a HLT is encountered or end–of–program is reached.
        /// </summary>
        public void Run()
        {
            while (Running && PC < Memory.ProgramSize)
            {
                Step();
                Thread.Sleep(500); // Slow execution for debugging.
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
                            Registers.GPR[destReg] = immediate;
                            Console.WriteLine($"[CPU] MOV: Set R{destReg} = {immediate}");
                        }
                        else if (destReg < Registers.GPR.Length + Registers.Graphics.Length)
                        {
                            int graphicsIndex = destReg - Registers.GPR.Length;
                            Registers.Graphics[graphicsIndex] = immediate;
                            Console.WriteLine($"[CPU] MOV: Set G{graphicsIndex} = {immediate}");
                        }
                        else
                        {
                            Console.WriteLine($"[CPU] MOV: Invalid register index {destReg}");
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
                            int before = Registers.GPR[regIndex];
                            Registers.GPR[regIndex] += immValue;
                            Console.WriteLine($"[CPU] ADDI: R{regIndex} ({before}) + {immValue} = {Registers.GPR[regIndex]}");

                            if (Registers.GPR[regIndex] == 0)
                                Registers.CpuFlag |= CpuFlags.Zero;
                            else
                                Registers.CpuFlag &= ~CpuFlags.Zero;
                        }
                        else if (regIndex < Registers.GPR.Length + Registers.Graphics.Length)
                        {
                            int graphicsIndex = regIndex - Registers.GPR.Length;
                            int before = Registers.Graphics[graphicsIndex];
                            Registers.Graphics[graphicsIndex] += immValue;
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
                            int before = Registers.GPR[regIndex];
                            Registers.GPR[regIndex] -= immValue;
                            Console.WriteLine($"[CPU] SUBI: R{regIndex} ({before}) - {immValue} = {Registers.GPR[regIndex]}");

                            if (Registers.GPR[regIndex] == 0)
                                Registers.CpuFlag |= CpuFlags.Zero;
                            else
                                Registers.CpuFlag &= ~CpuFlags.Zero;
                        }
                        else if (regIndex < Registers.GPR.Length + Registers.Graphics.Length)
                        {
                            int graphicsIndex = regIndex - Registers.GPR.Length;
                            int before = Registers.Graphics[graphicsIndex];
                            Registers.Graphics[graphicsIndex] -= immValue;
                            Console.WriteLine($"[CPU] SUBI: G{graphicsIndex} ({before}) - {immValue} = {Registers.Graphics[graphicsIndex]}");
                        }
                        else
                        {
                            Console.WriteLine($"[CPU] SUBI: Invalid register index {regIndex}");
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
                            if (Registers.GPR[regIndex] == immValue)
                            {
                                Registers.CpuFlag |= CpuFlags.Zero;
                                Console.WriteLine($"[CPU] CMPI: R{regIndex} equals {immValue}");
                            }
                            else
                            {
                                Registers.CpuFlag &= ~CpuFlags.Zero;
                                Console.WriteLine($"[CPU] CMPI: R{regIndex} does not equal {immValue}");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"[CPU] CMPI: Invalid register index {regIndex}");
                        }
                        break;
                    }

                case PromethiumOpcode.JMP:
                    {
                        // Format: [opcode][address (4 bytes)]
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
                case PromethiumOpcode.JNZ:
                    {
                        // Format: [opcode][address (4 bytes)]
                        int address = FetchInt();
                        if ((Registers.CpuFlag & CpuFlags.Zero) == 0)
                        {
                            Console.WriteLine($"[CPU] JNZ: Jumping to address {address}");
                            PC = address;
                        }
                        else
                        {
                            Console.WriteLine($"[CPU] JNZ: Not jumping to address {address} (zero flag set).");
                        }
                        break;
                    }
                case PromethiumOpcode.JZ:
                    {
                        // Format: [opcode][address (4 bytes)]
                        int address = FetchInt();
                        if ((Registers.CpuFlag & CpuFlags.Zero) != 0)
                        {
                            Console.WriteLine($"[CPU] JZ: Jumping to address {address}");
                            PC = address;
                        }
                        else
                        {
                            Console.WriteLine($"[CPU] JZ: Not jumping to address {address} (zero flag not set).");
                        }
                        break;
                    }

                case PromethiumOpcode.HLT:
                    {
                        Console.WriteLine("[CPU] HLT encountered. Halting execution.");
                        Running = false;
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
