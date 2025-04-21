using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

#pragma warning disable CS8618 //ts error pmo icl 
/// <summary>
/// the warning i disabled didnt seem to be needed so i disabled it HHAHAHAHAHA!

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
        public const int GeneralRegisterCount = 32;

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

            for (int lineNumber = 0; lineNumber < lines.Count; lineNumber++)
            {
                string trimmedLine = lines[lineNumber].Trim();

                // Remove comments (anything after ';').
                int commentIndex = trimmedLine.IndexOf(';');
                if (commentIndex >= 0)
                {
                    trimmedLine = trimmedLine.Substring(0, commentIndex).Trim();
                }

                // Skip empty lines after removing comments.
                if (string.IsNullOrEmpty(trimmedLine))
                    continue;

                // If the line declares a label (ends with ':'), record its address.
                if (trimmedLine.EndsWith(":"))
                {
                    string label = trimmedLine.Substring(0, trimmedLine.Length - 1);
                    labelTable[label] = currentAddress;
                    Console.WriteLine($"[PASM Loader] Label '{label}' at address {currentAddress} (Line {lineNumber + 1})");
                    continue;
                }

                // Otherwise, record the instruction and update the address by its size.
                processedLines.Add(trimmedLine);
                string mnemonic = trimmedLine.Split()[0].ToUpper();
                currentAddress += GetInstructionSize(mnemonic);
            }

            // Second pass: generate machine code.
            List<byte> programBytes = new List<byte>();

            for (int lineNumber = 0; lineNumber < processedLines.Count; lineNumber++)
            {
                string line = processedLines[lineNumber];
                string trimmedLine = line.Trim();

                // Split tokens by whitespace.
                string[] tokens = trimmedLine.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                string mnemonic = tokens[0].ToUpper();

                try
                {
                    // Convert mnemonic to opcode.
                    if (!Enum.TryParse<PromethiumOpcode>(mnemonic, out PromethiumOpcode opcode))
                    {
                        Console.WriteLine($"[PASM Loader] Unknown mnemonic: {mnemonic} (Line {lineNumber + 1})");
                        continue;
                    }

                    // Write the opcode (1 byte).
                    programBytes.Add((byte)opcode);

                    // Determine operand encoding based on mnemonic.
                    switch (mnemonic)
                    {
                        case "NOP":
                        case "RET":
                        case "HLT":
                        case "EI":
                        case "DI":
                        case "IRET":
                            break;

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
                            AddImmediateInstruction(tokens, programBytes);
                            break;

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

                        case "NOT":
                            AddRegisterInstruction(tokens, programBytes);
                            break;

                        case "CMP":
                            if (tokens.Length < 3)
                            {
                                Console.WriteLine($"[PASM Loader] Invalid CMP instruction: {string.Join(" ", tokens)} (Line {lineNumber + 1})");
                            }
                            else
                            {
                                byte reg1 = ParseRegisterToken(tokens[1].ToUpper());
                                byte reg2 = ParseRegisterToken(tokens[2].ToUpper());
                                programBytes.Add(reg1);
                                programBytes.Add(reg2);
                            }
                            break;

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

                        case "PUSH":
                        case "POP":
                            AddRegisterInstruction(tokens, programBytes);
                            break;

                        case "LOADI":
                        case "STOREI":
                            AddMemoryDomainInstruction(tokens, programBytes);
                            break;

                        case "IN":
                        case "OUT":
                            AddRegisterInstruction(tokens, programBytes);
                            break;

                        case "RAND":
                        case "TIME":
                            AddRegisterInstruction(tokens, programBytes);
                            break;

                        case "INT":
                            if (tokens.Length < 2)
                            {
                                Console.WriteLine($"[PASM Loader] Invalid INT instruction: {string.Join(" ", tokens)} (Line {lineNumber + 1})");
                            }
                            else
                            {
                                if (!byte.TryParse(tokens[1], out byte intNum))
                                {
                                    Console.WriteLine($"[PASM Loader] Invalid interrupt number: {tokens[1]} (Line {lineNumber + 1})");
                                    intNum = 0;
                                }
                                programBytes.Add(intNum);
                            }
                            break;

                        default:
                            Console.WriteLine($"[PASM Loader] Unhandled mnemonic: {mnemonic} (Line {lineNumber + 1})");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[PASM Loader] Error processing line {lineNumber + 1}: {line}");
                    Console.WriteLine($"[PASM Loader] Exception: {ex.Message}");
                }
            }

            return new PasmResult { ProgramBytes = programBytes.ToArray() };
        }

        /// <summary>
        /// Returns the size (in bytes) an instruction occupies based on its mnemonic.
        /// </summary>
        /// 
        
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
                
                    return 6; // [opcode][register][immediate (4 bytes)]
                             
                
                case "LOADI":
                case "STOREI":
                    return 7; // [opcode][domain (1 byte)][offset (4 bytes)][register (1 byte)]

                




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

        /// CPU expects: [opcode][register][immediate].
        /// </summary>
        private static void AddImmediateInstruction(string[] tokens, List<byte> programBytes)
        {
            if (tokens.Length < 3)
            {
                Console.WriteLine($"[PASM Loader] Invalid instruction: {string.Join(" ", tokens)}");
                return;
            }

            // Parse the immediate value
            int immediate;
            if (tokens[1].StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                // Parse hexadecimal literal
                if (!int.TryParse(tokens[1].Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out immediate))
                {
                    Console.WriteLine($"[PASM Loader] Invalid hexadecimal value: {tokens[1]}");
                    immediate = 0;
                }
            }
            else if (!int.TryParse(tokens[1], out immediate))
            {
                Console.WriteLine($"[PASM Loader] Invalid immediate value: {tokens[1]}");
                immediate = 0;
            }

            // Parse the register token
            string regToken = tokens[2].ToUpper();
            byte regByte = ParseRegisterToken(regToken);

            // Add the immediate value and register to the program bytes
            programBytes.AddRange(BitConverter.GetBytes(immediate));
            programBytes.Add(regByte);
        }


        /// <summary>
        /// Processes floating-point instructions and adds their binary representation to the program bytes.
        /// </summary>
        /// <param name="tokens">The instruction tokens (mnemonic and operands)</param>
        /// <param name="programBytes">The list of program bytes to append to</param>
        private static void AddFloatInstruction(string[] tokens, List<byte> programBytes)
        {
            // Expecting PASM syntax: MOVF <float> <register>
            if (tokens.Length < 3)
            {
                Console.WriteLine($"[PASM Loader] Invalid MOVF instruction: {string.Join(" ", tokens)}");
                return;
            }

            // Parse the float value using invariant culture.
            if (!float.TryParse(tokens[1], System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out float floatValue))
            {
                Console.WriteLine($"[PASM Loader] Invalid float value: {tokens[1]}");
                floatValue = 0f;
            }

            // Convert the float into 4 bytes in little-endian order.
            byte[] floatBytes = BitConverter.GetBytes(floatValue);
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(floatBytes);
            }
            // CPU expects: [opcode][float (4 bytes)][register]
            programBytes.AddRange(floatBytes);

            // Now parse the register operand.
            string regToken = tokens[2].ToUpper();
            byte regByte = ParseRegisterToken(regToken);
            programBytes.Add(regByte);
        }

        private static void ConvertRegisterToFloat(List<byte> programBytes, string regToken)
        {
            // ITOF converts an integer value in a register into a float.
            // CPU expects the format: [opcode][register]
            byte reg = ParseRegisterToken(regToken);
            programBytes.Add(reg);
            Console.WriteLine($"[PASM Loader] Converted register {regToken} to float (ITOF).");
        }

        private static void ConvertRegisterToInt(List<byte> programBytes, string regToken)
        {
            // FTOI converts a float value in a register into an integer (using floor).
            // CPU expects the format: [opcode][register]
            byte reg = ParseRegisterToken(regToken);
            programBytes.Add(reg);
            Console.WriteLine($"[PASM Loader] Converted register {regToken} to int (FTOI).");
        }
        /// <summary>
        /// Adds a float parameter to the program bytes.
        /// </summary>
        private static void AddFloatParameter(string token, List<byte> programBytes)
        {
            if (float.TryParse(token, out float value))
            {
                byte[] bytes = BitConverter.GetBytes(value);
                programBytes.AddRange(bytes);
            }
            else
            {
                Console.WriteLine($"[PASM Loader] Invalid float value: {token}");
                // Add a default value of 0.0f
                programBytes.AddRange(BitConverter.GetBytes(0.0f));
            }
        }

        /// <summary>
        /// Adds an integer parameter to the program bytes.
        /// </summary>
        private static void AddIntParameter(string token, List<byte> programBytes)
        {
            if (int.TryParse(token, out int value))
            {
                byte[] bytes = BitConverter.GetBytes(value);
                programBytes.AddRange(bytes);
            }
            else
            {
                Console.WriteLine($"[PASM Loader] Invalid integer value: {token}");
                // Add a default value of 0
                programBytes.AddRange(BitConverter.GetBytes(0));
            }
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

        // Add this method to handle memory domain instructions
        private static void AddMemoryDomainInstruction(string[] tokens, List<byte> programBytes)
        {
            if (tokens.Length < 4)
            {
                Console.WriteLine($"[PASM Loader] Invalid memory domain instruction: {string.Join(" ", tokens)}");
                return;
            }

            // Parse the memory domain
            if (!byte.TryParse(tokens[1], out byte domainByte))
            {
                Console.WriteLine($"[PASM Loader] Invalid memory domain: {tokens[1]}");
                domainByte = 1; // Default to SystemData
            }
            programBytes.Add(domainByte);

            // Determine if the offset is immediate or a register
            if (tokens[2].StartsWith("R", StringComparison.OrdinalIgnoreCase))
            {
                // Register-based offset
                programBytes.Add(1); // Offset type: 1 = Register
                byte offsetRegByte = ParseRegisterToken(tokens[2]); // Renamed to avoid conflict
                programBytes.Add(offsetRegByte);
            }
            else
            {
                // Immediate offset
                programBytes.Add(0); // Offset type: 0 = Immediate
                int offset;
                if (tokens[2].StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                {
                    // Parse hexadecimal offset
                    if (!int.TryParse(tokens[2].Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out offset))
                    {
                        Console.WriteLine($"[PASM Loader] Invalid hexadecimal offset: {tokens[2]}");
                        offset = 0;
                    }
                }
                else if (!int.TryParse(tokens[2], out offset))
                {
                    Console.WriteLine($"[PASM Loader] Invalid memory offset: {tokens[2]}");
                    offset = 0;
                }
                programBytes.AddRange(BitConverter.GetBytes(offset));
            }

            // Parse the register
            byte regByte = ParseRegisterToken(tokens[3]); // No conflict here
            programBytes.Add(regByte);
        }

        /// <summary>

        /// <summary>
        /// Parses a register token. "R" tokens return the register number.
        /// "G" tokens return the number offset by the general register count.
        /// </summary>

        private static byte ParseRegisterToken(string token)
        {
            if (token.StartsWith("R", StringComparison.OrdinalIgnoreCase))
            {
                if (int.TryParse(token.Substring(1), out int reg) && reg >= 0 && reg < GeneralRegisterCount)
                    return (byte)reg;
            }
            else if (token.StartsWith("G", StringComparison.OrdinalIgnoreCase))
            {
                if (int.TryParse(token.Substring(1), out int reg) && reg >= 0 && reg < GeneralRegisterCount)
                    return (byte)reg;
            }

            Console.WriteLine($"[PASM Loader] Invalid register token: {token}");
            return 0; // Default to R0 if invalid
        }


        /// <summary>
        /// Ensures a register token has the proper prefix (R or G).
        /// If it's just a number, assumes it's a G register for PDE operations.
        /// </summary>
        private static string EnsureRegisterPrefix(string token)
        {
            // If it already has a prefix, return it as is
            if (token.StartsWith("R") || token.StartsWith("G"))
                return token;

            // If it's a number, assume it's a G register for PDE operations
            if (int.TryParse(token, out _))
                return "G" + token;

            // Otherwise, return it unchanged
            return token;
        }


    }
}
