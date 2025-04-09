using System;
using System.Collections.Generic;

namespace PromethiumXS
{
    public class Cpu
    {
        /// <summary>
        /// Program Counter: Points to the current instruction in the System memory domain.
        /// </summary>
        public int PC { get; private set; }

        /// <summary>
        /// The CPU’s register file.
        /// </summary>
        public PromethiumRegisters Registers { get; private set; }

        /// <summary>
        /// The Memory instance for PromethiumXS.
        /// </summary>
        public Memory Memory { get; private set; }

        /// <summary>
        /// Indicates whether the CPU is running.
        /// </summary>
        public bool Running { get; private set; }

        // A simple call stack for storing return addresses.
        private List<int> callStack = new List<int>();

        public Cpu(Memory memory, PromethiumRegisters registers)
        {
            Memory = memory;
            Registers = registers;
            Reset();
        }

        /// <summary>
        /// Resets the CPU state, including registers and the call stack.
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
        /// Fetches a single byte from System memory and increments PC.
        /// Halts execution if PC reaches beyond the loaded program.
        /// </summary>
        private byte FetchByte()
        {
            if (PC >= Memory.ProgramSize)
            {
                Console.WriteLine("[CPU] End of program reached during FetchByte(). Halting.");
                Running = false;
                return 0;
            }
            byte b = Memory.Read(MemoryDomain.System, PC);
            PC++;
            return b;
        }

        /// <summary>
        /// Fetches a 32-bit integer (little-endian) from System memory and increments PC.
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
        /// Runs the CPU until a HLT instruction or end-of-program is reached.
        /// </summary>
        public void Run()
        {
            while (Running && PC < Memory.ProgramSize)
            {

                Step();
                Thread.Sleep(500);
            }
            Running = false;
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
                        byte destReg = FetchByte();
                        int immediate = FetchInt();
                        if (destReg < Registers.GPR.Length)
                        {
                            Registers.GPR[destReg] = immediate;
                            Console.WriteLine($"[CPU] MOV: Set R{destReg} = {immediate}");
                        }
                        else
                        {
                            Console.WriteLine($"[CPU] MOV: Invalid register index {destReg}");
                        }
                        break;
                    }

                case PromethiumOpcode.ADD:
                    {
                        byte regDest = FetchByte();
                        byte regSrc = FetchByte();
                        if (regDest < Registers.GPR.Length && regSrc < Registers.GPR.Length)
                        {
                            int before = Registers.GPR[regDest];
                            Registers.GPR[regDest] += Registers.GPR[regSrc];
                            Console.WriteLine($"[CPU] ADD: R{regDest} ({before}) + R{regSrc} ({Registers.GPR[regSrc]}) = {Registers.GPR[regDest]}");
                            if (Registers.GPR[regDest] == 0)
                                Registers.CpuFlag |= CpuFlags.Zero;
                            else
                                Registers.CpuFlag &= ~CpuFlags.Zero;
                        }
                        else
                        {
                            Console.WriteLine("[CPU] ADD: Invalid register indices.");
                        }
                        break;
                    }

                case PromethiumOpcode.SUB:
                    {
                        byte regDest = FetchByte();
                        byte regSrc = FetchByte();
                        if (regDest < Registers.GPR.Length && regSrc < Registers.GPR.Length)
                        {
                            int before = Registers.GPR[regDest];
                            Registers.GPR[regDest] -= Registers.GPR[regSrc];
                            Console.WriteLine($"[CPU] SUB: R{regDest} ({before}) - R{regSrc} ({Registers.GPR[regSrc]}) = {Registers.GPR[regDest]}");
                            if (Registers.GPR[regDest] == 0)
                                Registers.CpuFlag |= CpuFlags.Zero;
                            else
                                Registers.CpuFlag &= ~CpuFlags.Zero;
                        }
                        else
                        {
                            Console.WriteLine("[CPU] SUB: Invalid register indices.");
                        }
                        break;
                    }

                case PromethiumOpcode.ADDI:
                    {
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
                        else
                        {
                            Console.WriteLine($"[CPU] ADDI: Invalid register index {regIndex}");
                        }
                        break;
                    }

                case PromethiumOpcode.SUBI:
                    {
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
                        else
                        {
                            Console.WriteLine($"[CPU] SUBI: Invalid register index {regIndex}");
                        }
                        break;
                    }

                case PromethiumOpcode.JMP:
                    {
                        int address = FetchInt();
                        if (address >= 0 && address < (4 * 1024 * 1024))
                        {
                            Console.WriteLine($"[CPU] JMP: Jumping to address {address}");
                            PC = address;
                        }
                        else
                        {
                            Console.WriteLine($"[CPU] JMP: Invalid jump address {address}");
                        }
                        break;
                    }

                case PromethiumOpcode.CALL:
                    {
                        int address = FetchInt();
                        // Push the current PC onto our call stack.
                        callStack.Add(PC);
                        Console.WriteLine($"[CPU] CALL: Pushing return address {PC} and jumping to address {address}");
                        PC = address;
                        break;
                    }

                case PromethiumOpcode.RET:
                    {
                        if (callStack.Count > 0)
                        {
                            int retAddr = callStack[callStack.Count - 1];
                            callStack.RemoveAt(callStack.Count - 1);
                            Console.WriteLine($"[CPU] RET: Returning to address {retAddr}");
                            PC = retAddr;
                        }
                        else
                        {
                            Console.WriteLine("[CPU] RET: Call stack empty. Halting.");
                            Running = false;
                        }
                        break;
                    }

                case PromethiumOpcode.HLT:
                    {
                        Running = false;
                        Console.WriteLine("[CPU] HLT encountered. Halting execution.");
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
