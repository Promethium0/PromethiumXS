using System;
using System.Threading;
using System.Windows.Forms;
using OpenTK.Mathematics;

namespace PromethiumXS
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            // Step 1: Initialize the emulator components.
            PromethiumRegisters registers = new PromethiumRegisters();
            Memory memory = new Memory();
            Cpu cpu = new Cpu(memory, registers);

            // Step 2: Optionally, pre-fill graphics registers for testing.
            // For example, clearing the screen to black:
            registers.Graphics[0] = 1; // Clear screen opcode.
            registers.Graphics[1] = 0x000000FF; // Black color (packed RGBA).

            // Step 3: Launch the Windows Forms UI on a separate STA thread.
            Thread winFormsThread = new Thread(() =>
            {
                // Initialize Windows Forms configuration (if any).
                ApplicationConfiguration.Initialize();
                Application.Run(new RegisterDisplayForm(registers, memory, cpu));
            });
            winFormsThread.SetApartmentState(ApartmentState.STA);
            winFormsThread.Start();

        }
    }
}
