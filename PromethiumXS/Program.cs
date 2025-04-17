using System;
using System.Threading;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Text;

namespace PromethiumXS
{
    internal static class Program
    {
        // Import the AllocConsole function from the Windows API to open a console window.
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AllocConsole();

        [STAThread]
        static void Main()
        {
            // Step 1: Open a console window for logging/debugging.
            AllocConsole();
            Console.WriteLine("Console initialized. Type 'help' for a list of commands.");

            // Step 2: Initialize the emulator components.
            PromethiumRegisters registers = new PromethiumRegisters();
            Memory memory = new Memory();
            Cpu cpu = new Cpu(memory, registers);

            // Step 3: Launch the Windows Forms UI on a separate STA thread.
            Thread winFormsThread = new Thread(() =>
            {
                // Initialize Windows Forms configuration (if any).
                ApplicationConfiguration.Initialize();
                // Corrected line: Pass an instance of DisplayListManager instead of the type itself.
                DisplayListManager displayListManager = new DisplayListManager();
                Application.Run(new RegisterDisplayForm(registers, memory, cpu, displayListManager));
                
            });
            winFormsThread.SetApartmentState(ApartmentState.STA);
            winFormsThread.Start();

            // Step 4: Start the debugging console command loop.
            DebugConsoleLoop(registers, memory, cpu);
        }

        /// <summary>
        /// Handles debugging console commands.
        /// </summary>
        private static void DebugConsoleLoop(PromethiumRegisters registers, Memory memory, Cpu cpu)
        {
            Dictionary<MemoryDomain, int> memoryDumpOffsets = new(); // Tracks offsets for segmented dumps

            while (true)
            {
                Console.Write("> ");
                string input = Console.ReadLine()?.Trim();

                if (string.IsNullOrEmpty(input))
                    continue;

                string[] parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                string command = parts[0]; // Keep the command case-insensitive
                string[] args = parts.Length > 1 ? parts[1..] : Array.Empty<string>();

                switch (command.ToLower()) // Only convert the command to lowercase
                {
                    case "help":
                        PrintHelp();
                        break;

                    case "dumpmemory":
                        if (args.Length == 1 && Enum.TryParse(args[0], true, out MemoryDomain domain))
                        {
                            memoryDumpOffsets[domain] = 0; // Start at the beginning of the domain
                            DumpMemorySegment(memory, domain, memoryDumpOffsets);
                        }
                        else
                        {
                            Console.WriteLine("Usage: dumpmemory <domain>");
                            Console.WriteLine("Available domains: System, Video, Audio, DPL, Cartridge, IO, Cache, Scratch");
                        }
                        break;

                    case "next":
                        if (args.Length == 1 && Enum.TryParse(args[0], true, out MemoryDomain nextDomain))
                        {
                            if (memoryDumpOffsets.ContainsKey(nextDomain))
                            {
                                DumpMemorySegment(memory, nextDomain, memoryDumpOffsets);
                            }
                            else
                            {
                                Console.WriteLine($"No active memory dump for domain '{nextDomain}'. Use 'dumpmemory <domain>' first.");
                            }
                        }
                        else
                        {
                            Console.WriteLine("Usage: next <domain>");
                        }
                        break;

                    case "dumpregisters":
                        registers.Dump();
                        break;

                    case "resetcpu":
                        cpu.Reset();
                        Console.WriteLine("CPU reset.");
                        break;

                    case "run":
                        Console.WriteLine("Starting CPU execution...");
                        cpu.Run();
                        Console.WriteLine("CPU execution halted.");
                        break;

                    case "step":
                        Console.WriteLine("Executing one CPU step...");
                        cpu.Step();
                        break;

                    case "dumpopcodes":
                        if (args.Length == 2 && Enum.TryParse(args[0], true, out MemoryDomain opcodeDomain))
                        {
                            string filePath = args[1];
                            DumpOpcodesToFile(memory, opcodeDomain, filePath);
                        }
                        else
                        {
                            Console.WriteLine("Usage: dumpopcodes <domain> <filePath>");
                            Console.WriteLine("Available domains: System, Video, Audio, DPL, Cartridge, IO, Cache, Scratch");
                        }
                        break;


                    case "exit":
                        Console.WriteLine("Exiting debugger...");
                        Environment.Exit(0);
                        break;

                    case "searchdpl":
                        if (args.Length == 1)
                        {
                            string modelName = args[0]; // Keep the original case of the model name
                            SearchDplMemoryForModel(memory, modelName);
                        }
                        else
                        {
                            Console.WriteLine("Usage: searchdpl <modelName>");
                        }
                        break;

                    default:
                        Console.WriteLine($"Unknown command: {command}. Type 'help' for a list of commands.");
                        break;
                }
            }
        }



        /// <summary>
        /// Dumps a segment of memory for the specified domain.
        /// </summary>
        /// 
        public static void SearchDplMemoryForModel(Memory memory, string modelName)
        {
            byte[] dplMemory = memory.Domains[MemoryDomain.DPL];
            byte[] modelNameBytes = Encoding.ASCII.GetBytes(modelName);

            for (int i = 0; i <= dplMemory.Length - modelNameBytes.Length; i++)
            {
                if (dplMemory.Skip(i).Take(modelNameBytes.Length).SequenceEqual(modelNameBytes))
                {
                    Console.WriteLine($"[SearchDplMemoryForModel] Found model '{modelName}' at address {i:X8}");
                    return;
                }
            }

            Console.WriteLine($"[SearchDplMemoryForModel] Model '{modelName}' not found in DPL memory.");
        }

        private static void DumpMemorySegment(Memory memory, MemoryDomain domain, Dictionary<MemoryDomain, int> offsets)
        {
            const int SegmentSize = 16 * 20; // 20 segments of 16 bytes each
            int offset = offsets[domain];
            byte[] data = memory.Domains[domain];

            if (offset >= data.Length)
            {
                Console.WriteLine($"End of memory for {domain} domain.");
                return;
            }

            Console.WriteLine($"Memory Dump for {domain} Domain (Offset: {offset:X8}):");
            for (int i = 0; i < SegmentSize && offset < data.Length; i += 16, offset += 16)
            {
                int count = Math.Min(16, data.Length - offset);
                string hex = BitConverter.ToString(data, offset, count).Replace("-", " ");
                Console.WriteLine($"{offset:X8}: {hex}");
            }

            offsets[domain] = offset; // Update the offset for the next segment
            if (offset < data.Length)
            {
                Console.WriteLine($"Type 'next {domain}' to view the next segment.");
            }
            else
            {
                Console.WriteLine($"End of memory for {domain} domain.");
            }
        }

        private static void DumpOpcodesToFile(Memory memory, MemoryDomain domain, string filePath)
        {
            try
            {
                // Retrieve the memory data for the specified domain.
                byte[] data = memory.Domains[domain];

                // Open a StreamWriter to write the opcodes to the file.
                using StreamWriter writer = new StreamWriter(filePath);

                Console.WriteLine($"Dumping {domain} domain to file: {filePath}");

                // Iterate through the memory data and convert each byte to a Promethium opcode.
                for (int i = 0; i < data.Length; i++)
                {
                    // Convert the byte to a hexadecimal opcode.
                    string opcode = $"0x{data[i]:X2}";

                    // Write the opcode to the file.
                    writer.WriteLine(opcode);

                    // Optionally, log progress to the console.
                    if (i % 1024 == 0)
                    {
                        Console.WriteLine($"Processed {i}/{data.Length} bytes...");
                    }
                }

                Console.WriteLine($"Memory dump and opcode conversion completed. File saved to: {filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during memory dump: {ex.Message}");
            }
        }


        /// <summary>
        /// Prints the list of available debugging commands.
        /// </summary>
        private static void PrintHelp()
        {
            Console.WriteLine("Available commands:");
            Console.WriteLine("  help            - Show this help message.");
            Console.WriteLine("  dumpmemory <domain> - Dump the contents of a memory domain.");
            Console.WriteLine("                     Available domains: System, Video, Audio, DPL, Cartridge, IO, Cache, Scratch");
            Console.WriteLine("  next <domain>   - View the next segment of the memory dump for the specified domain.");
            Console.WriteLine("  dumpregisters   - Dump the current state of all registers.");
            Console.WriteLine("  resetcpu        - Reset the CPU and all registers.");
            Console.WriteLine("  run             - Start CPU execution.");
            Console.WriteLine("  step            - Execute a single CPU instruction.");
            Console.WriteLine("  exit            - Exit the debugger.");
        }
    }

}
