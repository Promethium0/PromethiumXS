using System;
using System.Drawing;
using System.Windows.Forms;

namespace PromethiumXS
{
    public partial class RegisterDisplayForm : Form
    {
        private PromethiumRegisters _registers;
        private Memory _memory;
        private Cpu _cpu;

        private DataGridView dgvGPR;
        private DataGridView dgvGraphics;
        private Label lblCpuFlags;
        private Label lblGfxFlags;
        private Button btnRefresh;
        private Button btnLoadPasm;
        private Button btnStart;

        // Removed GLControl from the form to avoid GLFW calls on a background thread.
        private Panel displayPlaceholder;

        public RegisterDisplayForm(PromethiumRegisters registers, Memory memory, Cpu cpu)
        {
            _registers = registers;
            _memory = memory;
            _cpu = cpu;
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            // Basic form configuration.
            this.Text = "PromethiumXS Register Display";
            this.Size = new Size(900, 700);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            // Setup for the general-purpose registers grid.
            dgvGPR = new DataGridView
            {
                Location = new Point(10, 10),
                Size = new Size(250, 300),
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                ColumnCount = 2
            };
            dgvGPR.Columns[0].Name = "Register";
            dgvGPR.Columns[1].Name = "Value";
            dgvGPR.Columns[0].Width = 100;
            dgvGPR.Columns[1].Width = 140;

            // Setup for the graphics registers grid.
            dgvGraphics = new DataGridView
            {
                Location = new Point(270, 10),
                Size = new Size(250, 150),
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                ColumnCount = 2
            };
            dgvGraphics.Columns[0].Name = "Register";
            dgvGraphics.Columns[1].Name = "Value";
            dgvGraphics.Columns[0].Width = 100;
            dgvGraphics.Columns[1].Width = 140;

            // CPU Flags label.
            lblCpuFlags = new Label
            {
                Location = new Point(10, 320),
                Size = new Size(250, 30),
                Font = new Font("Consolas", 10)
            };

            // Graphics Flags label.
            lblGfxFlags = new Label
            {
                Location = new Point(270, 170),
                Size = new Size(250, 30),
                Font = new Font("Consolas", 10)
            };

            // Refresh button.
            btnRefresh = new Button
            {
                Text = "Refresh",
                Location = new Point(270, 320)
            };
            btnRefresh.Click += BtnRefresh_Click;

            // Load PASM file button.
            btnLoadPasm = new Button
            {
                Text = "Load PASM File",
                Location = new Point(270, 360)
            };
            btnLoadPasm.Click += BtnLoadPasm_Click;

            // Start Program button.
            btnStart = new Button
            {
                Text = "Start Program",
                Location = new Point(270, 400)
            };
            btnStart.Click += BtnStart_Click;

            // Placeholder Panel for the 3D display.
            // This panel acts as a visual placeholder only.
            displayPlaceholder = new Panel
            {
                Location = new Point(10, 450),
                Size = new Size(860, 200),
                BackColor = Color.CornflowerBlue // Indicates the area reserved for 3D rendering.
            };

            // Add controls to the form.
            this.Controls.Add(dgvGPR);
            this.Controls.Add(dgvGraphics);
            this.Controls.Add(lblCpuFlags);
            this.Controls.Add(lblGfxFlags);
            this.Controls.Add(btnRefresh);
            this.Controls.Add(btnLoadPasm);
            this.Controls.Add(btnStart);
            this.Controls.Add(displayPlaceholder);

            // Initial display.
            RefreshDisplay();
        }

        private void BtnRefresh_Click(object sender, EventArgs e)
        {
            RefreshDisplay();
        }

        private void BtnLoadPasm_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "PASM files (*.proasm)|*.proasm|All files (*.*)|*.*";
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        PasmResult result = PasmLoader.AssembleFile(openFileDialog.FileName);

                        // Load the assembled program into System memory.
                        for (int i = 0; i < result.ProgramSize && i < (4 * 1024 * 1024); i++)
                        {
                            _memory.Domains[MemoryDomain.System][i] = result.ProgramBytes[i];
                        }

                        // Automatically set ProgramSize.
                        _memory.ProgramSize = result.ProgramSize;

                        // Reset the CPU.
                        _cpu.Reset();

                        MessageBox.Show("Program loaded successfully!", "Load PASM", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error loading PASM file: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private async void BtnStart_Click(object sender, EventArgs e)
        {
            btnStart.Enabled = false;
            await System.Threading.Tasks.Task.Run(() =>
            {
                while (_cpu.Running)
                {
                    _cpu.Step();
                    System.Threading.Thread.Sleep(500);
                    this.Invoke(new Action(RefreshDisplay));
                }
            });
            MessageBox.Show("Program execution halted.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            btnStart.Enabled = true;
        }

        private void RefreshDisplay()
        {
            dgvGPR.Rows.Clear();
            for (int i = 0; i < _registers.GPR.Length; i++)
                dgvGPR.Rows.Add($"R{i}", _registers.GPR[i]);

            dgvGraphics.Rows.Clear();
            for (int i = 0; i < _registers.Graphics.Length; i++)
                dgvGraphics.Rows.Add($"G{i}", _registers.Graphics[i]);

            lblCpuFlags.Text = "CPU Flags: " + _registers.CpuFlag.ToString();
            lblGfxFlags.Text = "GFX Flags: " + _registers.GraphicsFlag.ToString();
        }
    }
}
