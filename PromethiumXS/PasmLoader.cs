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
                    // Floating-point operations – format: [opcode][register (1 byte)][Float (4 bytes)]
                    case "FADD":
                    case "FSUB":
                    case "FMUL":
                    case "FDIV":
                    case "FAND":
                    case "FOR":
                    case "FXOR":
                    case "FSHL":
                    case "FSHR":
                    case "FSQRT":
                    case "FSIN":
                    case "FCOS":
                    case "FTAN":
                    case "FCMPEQ":
                        AddFloatInstruction(tokens, programBytes);
                        break;
                    case "MOVF":
                        AddFloatInstruction(tokens, programBytes);
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

                    case "ITOF":
                        ConvertRegisterToFloat(programBytes, tokens[1]);
                        break;
                    case "FTOI":
                        ConvertRegisterToInt(programBytes, tokens[1]);
                        break;
                    //Display list operations
                    case "DLSTART": //usage: DLSTART <MODELNAME>
                    case "DLPRIMITIVE": //usage: DLPRIMITIVE <PRIMITIVETYPE> 1 is triangle 2 is square 3 is polygon
                    case "DLVERTEX": //usage: DLVERTEX <X> <Y> <Z> //depending on the primitive type it could be more than 3 vertices
                    case "DLCOLOR": //usage: DLCOLOR <R> <G> <B>
                    case "DLEND": //usage: DLEND
                    case "STOREMODEL": //usage: STOREMODEL <REGISTER> <MODELNAME>
                    case "LOADMODEL": //usage: LOADMODEL <REGISTER> <MODELNAME>
                    case "DLCALL": //usage: DLCALL <MODELNAME> <X> <Y> <Z>
                        AddToDisplayList(tokens, programBytes);
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
                // Floating-point operations
                case "FADD":
                case "FSUB":
                case "FMUL":
                case "FDIV":
                case "FSQRT":
                case "FAND":
                case "FOR":
                case "FNOT":
                case "FXOR":
                case "FSIN":
                case "FCOS":
                case "FTAN":
                case "FSHL":
                case "FSHR":
                    return 6; // [opcode][register][Float (4 bytes)]


                case "MOVF":
                    return 6; // [opcode][register][Float (4 bytes)]



                // Other
                case "EI":
                case "DI":
                    return 1;

                case "ITOF": // Int to Float 
                    return 2; //return 2 because it targets a register
                case "FTOI": // Float to Int just floors the register
                    return 2; //return 2 because it targets a register 

                case "DLSTART":
                    // Not fixed; depends on the model name length.
                    return 0;
                case "DLPRIMITIVE":
                    return 1;
                case "DLCOLOR":
                    return 3;
                case "DLVERTEX":
                    return 12;
                case "DLEND":
                    return 0;
                case "DLCALL":
                    // Fixed portion (12 bytes for coordinates) plus the model name.
                    return 0;
                case "STOREMODEL":
                    return 3; // Base size (opcode + register + name length) + model name length (variable)

                case "LOADMODEL":
                    return 14; // Fixed size (opcode + register + 3 floats for coordinates)

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
        /// Adds a Model to a display list like this
        /// DLSTART <MODEL_NAME>
        /// DLPRIMITIVE 1 (1 means triangle) so its made up of 3 triangles 
        /// DLCOLOR<COLORHEX>
        ///DLVERTEX <X1>,<Y1>,<Z1>
        ///DLVERTEX<X2>,<Y2>,<Z2>
        ///DLVERTEX<X3>,<Y3>,<Z3>
        ///DLEND
        ///
        /// 
        /// DLCALL <MODELNAME>
        /// dl call means it loads the model into the render pipeline if you want to load the model in at a specific point its DLCALL MODEL_NAME X Y Z by default it loads it at 0,0,0
        /// </summary>
        /// <param name="tokens"></param>
        /// <param name="programBytes"></param>
        public static void AddToDisplayList(string[] tokens, List<byte> programBytes, int videoMemoryStartAddress = 0)
        {
            string mnemonic = tokens[0].ToUpper();
            switch (mnemonic)
            {
                case "DLSTART":
                    if (tokens.Length < 2)
                    {
                        Console.WriteLine("[PASM Loader] DLSTART requires a model name");
                        break;
                    }
                    {
                        string modelName = tokens[1];
                        byte nameLength = (byte)modelName.Length;
                        programBytes.Add(nameLength);
                        programBytes.AddRange(Encoding.ASCII.GetBytes(modelName));
                    }
                    break;

                case "DLPRIMITIVE":
                    if (tokens.Length < 2)
                    {
                        Console.WriteLine("[PASM Loader] DLPRIMITIVE requires a primitive type");
                        break;
                    }
                    {
                        if (!byte.TryParse(tokens[1], out byte primitive))
                        {
                            Console.WriteLine($"[PASM Loader] Invalid primitive type: {tokens[1]}");
                            primitive = 0;
                        }
                        programBytes.Add(primitive);
                        // Set primitive type in video memory
                        programBytes[videoMemoryStartAddress] = primitive; // Assuming primitiveType is at offset 0
                    }
                    break;

                case "DLCOLOR":
                    if (tokens.Length < 2)
                    {
                        Console.WriteLine("[PASM Loader] DLCOLOR requires a hex color");
                        break;
                    }
                    else
                    {
                        string colorHex = tokens[1];
                        if (colorHex.Length != 6)
                        {
                            Console.WriteLine($"[PASM Loader] Invalid color hex length: {colorHex}");
                            break;
                        }
                        try
                        {
                            byte r = Convert.ToByte(colorHex.Substring(0, 2), 16);
                            byte g = Convert.ToByte(colorHex.Substring(2, 2), 16);
                            byte b = Convert.ToByte(colorHex.Substring(4, 2), 16);
                            programBytes.Add(r);
                            programBytes.Add(g);
                            programBytes.Add(b);
                            // Increment color count in video memory
                            programBytes[videoMemoryStartAddress + 5]++; // Assuming colorCount is at offset 5
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[PASM Loader] Error parsing color: {ex.Message}");
                        }
                    }
                    break;

                case "DLVERTEX":
                    if (tokens.Length < 4)
                    {
                        Console.WriteLine("[PASM Loader] DLVERTEX requires 3 coordinates (X Y Z)");
                        break;
                    }
                    else
                    {
                        for (int i = 1; i <= 3; i++)
                        {
                            if (!float.TryParse(tokens[i], NumberStyles.Float, CultureInfo.InvariantCulture, out float coord))
                            {
                                Console.WriteLine($"[PASM Loader] Invalid coordinate value: {tokens[i]}");
                                coord = 0f;
                            }
                            programBytes.AddRange(BitConverter.GetBytes(coord));
                        }
                        // Increment vertex count in video memory
                        programBytes[videoMemoryStartAddress + 1]++; // Assuming vertexCount is at offset 1
                    }
                    break;

                case "DLEND":
                    // DLEND carries no additional data.
                    break;

                case "DLCALL":
                    if (tokens.Length < 2)
                    {
                        Console.WriteLine("[PASM Loader] DLCALL requires a model name");
                        break;
                    }
                    else
                    {
                        // Encode the model name as in DLSTART.
                        string modelName = tokens[1];
                        byte nameLength = (byte)modelName.Length;
                        programBytes.Add(nameLength);
                        programBytes.AddRange(Encoding.ASCII.GetBytes(modelName));

                        //coordinates; if not provided, use (0,0,0).
                        float x = 0f, y = 0f, z = 0f;
                        if (tokens.Length >= 5)
                        {
                            if (!float.TryParse(tokens[2], NumberStyles.Float, CultureInfo.InvariantCulture, out x))
                            {
                                Console.WriteLine($"[PASM Loader] Invalid X coordinate: {tokens[2]}");
                            }
                            if (!float.TryParse(tokens[3], NumberStyles.Float, CultureInfo.InvariantCulture, out y))
                            {
                                Console.WriteLine($"[PASM Loader] Invalid Y coordinate: {tokens[3]}");
                            }
                            if (!float.TryParse(tokens[4], NumberStyles.Float, CultureInfo.InvariantCulture, out z))
                            {
                                Console.WriteLine($"[PASM Loader] Invalid Z coordinate: {tokens[4]}");
                            }
                        }
                        programBytes.AddRange(BitConverter.GetBytes(x));
                        programBytes.AddRange(BitConverter.GetBytes(y));
                        programBytes.AddRange(BitConverter.GetBytes(z));
                    }
                    break;

                case "STOREMODEL":
                    if (tokens.Length < 3)
                    {
                        Console.WriteLine("[PASM Loader] STOREMODEL requires a register and a model name.");
                        break;
                    }
                    {
                        byte regIndex = ParseRegisterToken(tokens[1].ToUpper());
                        string modelName = tokens[2];
                        byte nameLength = (byte)modelName.Length;
                        programBytes.Add(regIndex);
                        programBytes.Add(nameLength);
                        programBytes.AddRange(Encoding.ASCII.GetBytes(modelName));
                    }
                    break;

                case "LOADMODEL":
                    if (tokens.Length < 5)
                    {
                        Console.WriteLine("[PASM Loader] LOADMODEL requires a register and 3 coordinates (X Y Z).");
                        break;
                    }
                    {
                        byte regIndex = ParseRegisterToken(tokens[1].ToUpper());
                        programBytes.Add(regIndex);
                        for (int i = 2; i <= 4; i++)
                        {
                            if (!float.TryParse(tokens[i], NumberStyles.Float, CultureInfo.InvariantCulture, out float coord))
                            {
                                Console.WriteLine($"[PASM Loader] Invalid coordinate value: {tokens[i]}");
                                coord = 0f;
                            }
                            programBytes.AddRange(BitConverter.GetBytes(coord));
                        }
                    }
                    break;

                default:
                    Console.WriteLine($"[PASM Loader] Unknown display list mnemonic: {mnemonic}");
                    break;
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
