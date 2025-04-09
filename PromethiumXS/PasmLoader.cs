using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PromethiumXS
{
    /// <summary>
    /// Result object returned by the PASM loader, encapsulating the generated machine code and its size.
    /// </summary>
    public class PasmResult
    {
        public byte[] ProgramBytes { get; set; }
        public int ProgramSize => ProgramBytes.Length; // Auto-calculate size.
    }

    /// <summary>
    /// A simple assembler for PASM files that supports labels.
    /// </summary>
    public static class PasmLoader
    {
        /// <summary>
        /// Assembles a PASM file into machine code, resolving labels and calculating program size.
        /// </summary>
        /// <param name="filePath">The path to the PASM file.</param>
        /// <returns>A PasmResult containing the machine code and its size.</returns>
        public static PasmResult AssembleFile(string filePath)
        {
            // Read all lines from the PASM file.
            List<string> lines = File.ReadAllLines(filePath).ToList();

            // First pass: collect labels.
            Dictionary<string, int> labelTable = new Dictionary<string, int>();
            int currentAddress = 0;
            List<string> processedLines = new List<string>();

            foreach (string line in lines)
            {
                string trimmedLine = line.Trim();
                if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith(";"))
                    continue;

                // If this is a label declaration (ends with a colon)
                if (trimmedLine.EndsWith(":"))
                {
                    string label = trimmedLine.Substring(0, trimmedLine.Length - 1);
                    // Record the current address for this label.
                    labelTable[label] = currentAddress;
                    continue;
                }

                // Otherwise, keep this line for the second pass.
                processedLines.Add(trimmedLine);

                // Determine the size of this instruction and update currentAddress.
                string[] tokens = trimmedLine.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                string mnemonic = tokens[0].ToUpper();
                int instSize = GetInstructionSize(mnemonic);
                currentAddress += instSize;
            }

            // Second pass: generate machine code, resolving labels.
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
                    Console.WriteLine($"Unknown mnemonic: {mnemonic}");
                    continue;
                }

                // Write opcode (1 byte)
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
                        // Immediate instructions: [opcode][register][immediate (4 bytes)].
                        AddImmediateInstruction(tokens, programBytes);
                        break;

                    case "ADD":
                    case "SUB":
                        // Register-to-register instructions: [opcode][destReg][sourceReg].
                        AddRegisterInstruction(tokens, programBytes);
                        break;

                    case "JMP":
                    case "CALL":
                        // Jump/call instructions: [opcode][address (4 bytes)].
                        AddLabelInstruction(tokens, labelTable, programBytes);
                        break;

                    default:
                        break;
                }
            }

            return new PasmResult { ProgramBytes = programBytes.ToArray() };
        }

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
                    return 6; // [opcode][register][immediate (4 bytes)].
                case "ADD":
                case "SUB":
                    return 3; // [opcode][destReg][sourceReg].
                case "JMP":
                case "CALL":
                    return 5; // [opcode][address (4 bytes)].
                default:
                    return 1; // Unknown instructions default to 1 byte.
            }
        }

        private static void AddImmediateInstruction(string[] tokens, List<byte> programBytes)
        {
            if (tokens.Length < 3)
            {
                Console.WriteLine($"Invalid instruction: {string.Join(" ", tokens)}");
                return;
            }

            if (!int.TryParse(tokens[1], out int immediate))
            {
                Console.WriteLine($"Invalid immediate value: {tokens[1]}");
                immediate = 0;
            }

            string regToken = tokens[2].ToUpper();
            if (regToken.StartsWith("R") && int.TryParse(regToken.Substring(1), out int reg))
            {
                programBytes.Add((byte)reg);
            }
            else
            {
                Console.WriteLine($"Invalid register: {tokens[2]}");
                programBytes.Add(0);
            }

            programBytes.AddRange(BitConverter.GetBytes(immediate));
        }

        private static void AddRegisterInstruction(string[] tokens, List<byte> programBytes)
        {
            if (tokens.Length < 3)
            {
                Console.WriteLine($"Invalid instruction: {string.Join(" ", tokens)}");
                return;
            }

            if (tokens[1].ToUpper().StartsWith("R") && int.TryParse(tokens[1].Substring(1), out int destReg))
            {
                programBytes.Add((byte)destReg);
            }
            else
            {
                Console.WriteLine($"Invalid register: {tokens[1]}");
                programBytes.Add(0);
            }

            if (tokens[2].ToUpper().StartsWith("R") && int.TryParse(tokens[2].Substring(1), out int srcReg))
            {
                programBytes.Add((byte)srcReg);
            }
            else
            {
                Console.WriteLine($"Invalid register: {tokens[2]}");
                programBytes.Add(0);
            }
        }

        private static void AddLabelInstruction(string[] tokens, Dictionary<string, int> labelTable, List<byte> programBytes)
        {
            if (tokens.Length < 2)
            {
                Console.WriteLine($"Invalid instruction: {string.Join(" ", tokens)}");
                return;
            }

            string label = tokens[1];
            if (labelTable.TryGetValue(label, out int address))
            {
                programBytes.AddRange(BitConverter.GetBytes(address));
            }
            else
            {
                Console.WriteLine($"Label not found: {label}");
                programBytes.AddRange(BitConverter.GetBytes(0));
            }
        }
    }
}
