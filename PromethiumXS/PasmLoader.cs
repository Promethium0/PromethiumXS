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
        // Number of GPR registers (used when parsing register tokens).
        private const int GeneralRegisterCount = 32;

        /// <summary>
        /// Assembles a PASM file into machine code, resolving labels.
        /// </summary>
        public static PasmResult AssembleFile(string filePath)
        {
            // Read all lines from the file.
            List<string> lines = File.ReadAllLines(filePath).ToList();

            // First pass: collect labels and calculate their addresses.
            Dictionary<string, int> labelTable = new Dictionary<string, int>();
            int currentAddress = 0;
            List<string> processedLines = new List<string>();

            foreach (string line in lines)
            {
                string trimmedLine = line.Trim();

                // Skip empty and comment lines.
                if (string.IsNullOrEmpty(trimmedLine) ||
                    trimmedLine.StartsWith(";") ||
                    (trimmedLine.StartsWith(":") && !trimmedLine.EndsWith(":")))
                {
                    continue;
                }

                // If the line is a label declaration (ends with ':').
                if (trimmedLine.EndsWith(":"))
                {
                    string label = trimmedLine.Substring(0, trimmedLine.Length - 1);
                    labelTable[label] = currentAddress;
                    Console.WriteLine($"[PASM Loader] Label '{label}' at address {currentAddress}");
                    continue;
                }

                // Keep the instruction; update the currentAddress based on its size.
                processedLines.Add(trimmedLine);
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

                string[] tokens = trimmedLine.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                string mnemonic = tokens[0].ToUpper();

                if (!Enum.TryParse<PromethiumOpcode>(mnemonic, out PromethiumOpcode opcode))
                {
                    Console.WriteLine($"[PASM Loader] Unknown mnemonic: {mnemonic}");
                    continue;
                }

                // Write the opcode (1 byte).
                programBytes.Add((byte)opcode);

                switch (mnemonic)
                {
                    case "NOP":
                    case "RET":
                    case "HLT":
                        break;

                    case "MOV":
                    case "ADDI":
                    case "SUBI":
                    case "CMPI":
                    case "LI":
                        // Immediate instructions: [opcode][register (1 byte)][immediate (4 bytes)]
                        AddImmediateInstruction(tokens, programBytes);
                        break;

                    case "ADD":
                    case "SUB":
                        // Register-to-register instructions: [opcode][destReg][srcReg]
                        AddRegisterInstruction(tokens, programBytes);
                        break;

                    case "JMP":
                    case "JZ":
                    case "JNZ":
                    case "JE":
                    case "JNE":
                    case "JG":
                    case "JL":
                    case "JLE":
                    case "JGE":
                        
                    case "CALL":
                        // Jump instructions: [opcode][address (4 bytes)]
                        AddLabelInstruction(tokens, labelTable, programBytes);
                        break;

                    default:
                        Console.WriteLine($"[PASM Loader] Unhandled mnemonic: {mnemonic}");
                        break;
                }
            }

            return new PasmResult { ProgramBytes = programBytes.ToArray() };
        }

        /// <summary>
        /// Gets the size (in bytes) of an instruction based on its mnemonic.
        /// </summary>
        private static int GetInstructionSize(string mnemonic)
        {
            switch (mnemonic)
            {
                case "NOP":
                case "RET":
                case "HLT":
                    return 1;
                case "MOV":
                case "ADDI":
                case "SUBI":
                case "CMPI":
                case "LI":
                    return 6;  // [opcode][register(1)][immediate(4)]
                case "ADD":
                case "SUB":
                    return 3;  // [opcode][destReg][srcReg]
                case "JMP":
                case "JZ":
                case "JNZ":
                case "JE":
                case "JNE":
                case "JG":
                case "JL":
                case "JLE":
                case "JGE":

                case "CALL":
                    return 5;  // [opcode][address(4)]
                default:
                    return 1;
            }
        }

        /// <summary>
        /// Adds an immediate instruction.
        /// PASM syntax: immediate value first, then the register.
        /// CPU expects: [opcode][register][immediate]
        /// </summary>
        private static void AddImmediateInstruction(string[] tokens, List<byte> programBytes)
        {
            if (tokens.Length < 3)
            {
                Console.WriteLine($"[PASM Loader] Invalid instruction: {string.Join(" ", tokens)}");
                return;
            }

            if (!int.TryParse(tokens[1], out int immediate))
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
        /// Format: [opcode][destReg][srcReg]
        /// </summary>
        private static void AddRegisterInstruction(string[] tokens, List<byte> programBytes)
        {
            if (tokens.Length < 3)
            {
                Console.WriteLine($"[PASM Loader] Invalid instruction: {string.Join(" ", tokens)}");
                return;
            }

            byte destReg = ParseRegisterToken(tokens[1].ToUpper());
            byte srcReg = ParseRegisterToken(tokens[2].ToUpper());
            programBytes.Add(destReg);
            programBytes.Add(srcReg);
        }

        /// <summary>
        /// Parses a register token.
        /// "R" tokens are returned as-is,
        /// "G" tokens are offset by the number of general registers.
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

        /// <summary>
        /// Adds a label-based instruction (e.g., JMP or CALL).
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
    }
}
