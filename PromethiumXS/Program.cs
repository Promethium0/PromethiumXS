namespace PromethiumXS
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Initialize the emulator components
            PromethiumRegisters registers = new PromethiumRegisters();
            Memory memory = new Memory();
            Cpu cpu = new Cpu(memory, registers);

            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();

            // Pass the required components to the RegisterDisplayForm constructor
            Application.Run(new RegisterDisplayForm(registers, memory, cpu));
        }
    }
}
