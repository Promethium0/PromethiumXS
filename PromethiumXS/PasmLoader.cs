using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PromethiumXS
{
    public class PasmResult
    {
        public byte[] ProgramBytes { get; set; }
        public int ProgramSize => ProgramBytes.Length;
    }

    public static class PasmLoader
    {
        // Used when parsing register tokens (e.g. "R0", "G1", etc.)
        private const int GeneralRegisterCount = 32;

        /// <summary>
        /// Assembles a PASM file into machine code, resolving labels.
        /// </summary>
        public static PasmResult AssembleFile(string filePath)
        {
            // Read every line from the PASM file.
            List<string> lines = File.ReadAllLines(filePath).ToList();

            // First pass: collect labels and compute the current address for each.
            Dictionary<string, int> labelTable = new Dictionary<string, int>();
            int currentAddress = 0;
            List<string> processedLines = new List<string>();

            foreach (string line in lines)
            {
                string trimmedLine = line.Trim();

                // Skip empty and comment lines.
                if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith(";"))
                    continue;

                // If the line declares a label (ends with ':'), record its address.
                if (trimmedLine.EndsWith(":"))
                {
                    string label = trimmedLine.Substring(0, trimmedLine.Length - 1);
                    labelTable[label] = currentAddress;
                    Console.WriteLine($"[PASM Loader] Label '{label}' at address {currentAddress}");
                    continue;
                }

                // Otherwise, record the instruction and update the address by its size.
                processedLines.Add(trimmedLine);
                // The first token is the mnemonic.
                string mnemonic = trimmedLine.Split()[0].ToUpper();
                currentAddress += GetInstructionSize(mnemonic);
            }

            // Second pass: generate machine code.
            List<byte> programBytes = new List<byte>();

            foreach (string line in processedLines)
            {
                string trimmedLine = line.Trim();
                if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith(";"))
                    continue;

                // Split tokens by whitespace.
                string[] tokens = trimmedLine.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                string mnemonic = tokens[0].ToUpper();

                // Convert mnemonic to opcode.
                if (!Enum.TryParse<PromethiumOpcode>(mnemonic, out PromethiumOpcode opcode))
                {
                    Console.WriteLine($"[PASM Loader] Unknown mnemonic: {mnemonic}");
                    continue;
                }

                // Write the opcode (1 byte).
                programBytes.Add((byte)opcode);

                // Determine operand encoding based on mnemonic.
                switch (mnemonic)
                {
                    // No-operand instructions.
                    case "NOP":
                    case "RET":
                    case "HLT":
                    case "EI":
                    case "DI":
                    case "IRET":
                        break;

                    // Immediate instructions – format: [opcode][register (1 byte)][immediate (4 bytes)]
                    case "MOV":
                    case "ADDI":
                    case "SUBI":
                    case "MULI":
                    case "DIVI":
                    case "ANDI":
                    case "ORI":
                    case "XORI":
                    case "SHLI":
                    case "SHRI":
                    case "CMPI":
                    case "LI":
                    case "MODI":
                    case "LOADI":
                    case "STOREI":
                        AddImmediateInstruction(tokens, programBytes);
                        break;

                    // Register-to-register operations – format: [opcode][reg1][reg2]
                    case "ADD":
                    case "SUB":
                    case "MUL":
                    case "DIV":
                    case "MOD":
                    case "AND":
                    case "OR":
                    case "XOR":
                    case "SHL":
                    case "SHR":
                        AddRegisterInstruction(tokens, programBytes);
                        break;

                    // Unary register operation.
                    case "NOT":
                        // Format: [opcode][register]
                        AddRegisterInstruction(tokens, programBytes);
                        break;

                    // Comparison: two registers.
                    case "CMP":
                        if (tokens.Length < 3)
                        {
                            Console.WriteLine($"[PASM Loader] Invalid CMP instruction: {string.Join(" ", tokens)}");
                        }
                        else
                        {
                            byte reg1 = ParseRegisterToken(tokens[1].ToUpper());
                            byte reg2 = ParseRegisterToken(tokens[2].ToUpper());
                            programBytes.Add(reg1);
                            programBytes.Add(reg2);
                        }
                        break;

                    // Jump and call instructions – format: [opcode][address (4 bytes)]
                    case "JMP":
                    case "JZ":
                    case "JNZ":
                    case "JE":
                    case "JNE":
                    case "JG":
                    case "JL":
                    case "JGE":
                    case "JLE":
                    case "CALL":
                        AddLabelInstruction(tokens, labelTable, programBytes);
                        break;

                    // Stack operations – format: [opcode][register]
                    case "PUSH":
                    case "POP":
                        AddRegisterInstruction(tokens, programBytes);
                        break;

                    // Memory LOAD/STORE – assumed format: [opcode][register][address (4 bytes)]
                    case "LOAD":
                    case "STORE":
                        AddImmediateInstruction(tokens, programBytes);
                        break;

                    // I/O operations – assumed format: [opcode][register]
                    case "IN":
                    case "OUT":
                        AddRegisterInstruction(tokens, programBytes);
                        break;

                    // Special operations that use a register operand – format: [opcode][register]
                    case "RAND":
                    case "TIME":
                        AddRegisterInstruction(tokens, programBytes);
                        break;

                    // Interrupt operations.
                    case "INT":
                        // Format: [opcode][interrupt number (1 byte)]
                        if (tokens.Length < 2)
                        {
                            Console.WriteLine($"[PASM Loader] Invalid INT instruction: {string.Join(" ", tokens)}");
                        }
                        else
                        {
                            if (!byte.TryParse(tokens[1], out byte intNum))
                            {
                                Console.WriteLine($"[PASM Loader] Invalid interrupt number: {tokens[1]}");
                                intNum = 0;
                            }
                            programBytes.Add(intNum);
                        }
                        break;

                    default:
                        Console.WriteLine($"[PASM Loader] Unhandled mnemonic: {mnemonic}");
                        break;
                }
            }

            return new PasmResult { ProgramBytes = programBytes.ToArray() };
        }

        /// <summary>
        /// Returns the size (in bytes) an instruction occupies based on its mnemonic.
        /// </summary>
        private static int GetInstructionSize(string mnemonic)
        {
            switch (mnemonic)
            {
                // Basic operations
                case "NOP":
                    return 1;
                case "MOV":
                    return 6; // [opcode][register][immediate (4 bytes)]

                // Memory operations
                case "LOAD":
                case "STORE":
                    return 6; // [opcode][register][address (4 bytes)]

                // Arithmetic operations (register-to-register)
                case "ADD":
                case "SUB":
                case "MUL":
                case "DIV":
                case "MOD":
                    return 3; // [opcode][destReg][srcReg]

                // Bitwise operations
                case "AND":
                case "OR":
                case "XOR":
                case "SHL":
                case "SHR":
                    return 3; // [opcode][destReg][srcReg]
                case "NOT":
                    return 2; // [opcode][register]

                // Control flow operations
                case "CMP":
                    return 3; // [opcode][reg1][reg2]
                case "JMP":
                case "JZ":
                case "JNZ":
                case "JE":
                case "JNE":
                case "JG":
                case "JL":
                case "JGE":
                case "JLE":
                case "CALL":
                    return 5; // [opcode][address (4 bytes)]

                // Subroutine operations
                case "RET":
                    return 1;

                // Stack operations
                case "PUSH":
                case "POP":
                    return 2; // [opcode][register]

                // I/O operations
                case "IN":
                case "OUT":
                    return 2; // [opcode][register]

                // Special operations
                case "HLT":
                    return 1;
                case "RAND":
                case "TIME":
                    return 2; // [opcode][register]

                // Interrupt operations
                case "INT":
                    return 2; // [opcode][interrupt number (1 byte)]
                case "IRET":
                    return 1;

                // Immediate operations
                case "ADDI":
                case "SUBI":
                case "MULI":
                case "DIVI":
                case "ANDI":
                case "ORI":
                case "XORI":
                case "SHLI":
                case "SHRI":
                case "CMPI":
                case "LI":
                case "MODI":
                case "LOADI":
                case "STOREI":
                    return 6; // [opcode][register][immediate (4 bytes)]

                // Other
                case "EI":
                case "DI":
                    return 1;

                default:
                    return 1;
            }
        }

        /// <summary>
        /// Adds an immediate instruction to the program bytes.
        /// PASM syntax is assumed to be: [mnemonic] [immediate] [register].
        /// CPU expects: [opcode][register][immediate].
        /// </summary>
        private static void AddImmediateInstruction(string[] tokens, List<byte> programBytes)
        {
            if (tokens.Length < 3)
            {
                Console.WriteLine($"[PASM Loader] Invalid instruction: {string.Join(" ", tokens)}");
                return;
            }

            int immediate;
            if (tokens[1].StartsWith("0b"))
            {
                // Parse binary literal: remove "0b" and convert the rest from binary to integer
                string binaryLiteral = tokens[1].Substring(2); // Remove "0b"
                try
                {
                    immediate = Convert.ToInt32(binaryLiteral, 2); // Base 2 conversion
                }
                catch
                {
                    Console.WriteLine($"[PASM Loader] Invalid binary literal: {tokens[1]}");
                    immediate = 0;
                }
            }
            else if (int.TryParse(tokens[1], out immediate))
            {
                // Handle decimal and other numerical formats
            }
            else
            {
                Console.WriteLine($"[PASM Loader] Invalid immediate value: {tokens[1]}");
                immediate = 0;
            }

            string regToken = tokens[2].ToUpper();
            byte regByte = ParseRegisterToken(regToken);
            programBytes.Add(regByte);
            programBytes.AddRange(BitConverter.GetBytes(immediate));
        }



        /// <summary>
        /// Adds a register-to-register instruction.
        /// If two register operands are provided, the format is: [opcode][reg1][reg2].
        /// If only one is provided, it’s treated as a single register operand (for unary operations).
        /// </summary>
        private static void AddRegisterInstruction(string[] tokens, List<byte> programBytes)
        {
            if (tokens.Length < 2)
            {
                Console.WriteLine($"[PASM Loader] Invalid register instruction: {string.Join(" ", tokens)}");
                return;
            }

            if (tokens.Length >= 3)
            {
                byte reg1 = ParseRegisterToken(tokens[1].ToUpper());
                byte reg2 = ParseRegisterToken(tokens[2].ToUpper());
                programBytes.Add(reg1);
                programBytes.Add(reg2);
            }
            else
            {
                byte reg = ParseRegisterToken(tokens[1].ToUpper());
                programBytes.Add(reg);
            }
        }

        /// <summary>
        /// Adds a label-based instruction operand.
        /// For jump and call instructions the label is resolved into an address.
        /// </summary>
        private static void AddLabelInstruction(string[] tokens, Dictionary<string, int> labelTable, List<byte> programBytes)
        {
            if (tokens.Length < 2)
            {
                Console.WriteLine($"[PASM Loader] Invalid instruction: {string.Join(" ", tokens)}");
                return;
            }

            string label = tokens[1];
            if (labelTable.TryGetValue(label, out int address))
            {
                programBytes.AddRange(BitConverter.GetBytes(address));
            }
            else
            {
                Console.WriteLine($"[PASM Loader] Label not found: {label}. Using address 0.");
                programBytes.AddRange(BitConverter.GetBytes(0));
            }
        }

        /// <summary>
        /// Parses a register token. "R" tokens return the register number.
        /// "G" tokens return the number offset by the general register count.
        /// </summary>
        private static byte ParseRegisterToken(string token)
        {
            if (token.StartsWith("R"))
            {
                if (int.TryParse(token.Substring(1), out int reg))
                    return (byte)reg;
            }
            else if (token.StartsWith("G"))
            {
                if (int.TryParse(token.Substring(1), out int graphicsReg))
                    return (byte)(GeneralRegisterCount + graphicsReg);
            }
            Console.WriteLine($"[PASM Loader] Invalid register token: {token}");
            return 0;
        }
    }
}
