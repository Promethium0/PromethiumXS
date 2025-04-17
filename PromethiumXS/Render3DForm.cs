using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PromethiumXS
{
    public partial class Render3DForm : Form
    {
        public Render3DForm()
        {
            InitializeComponent();
        }
    }
    public class Renderer3DForm : Form
    {
        private Renderer3D _renderer3D;

        public Renderer3DForm(DisplayListManager displayListManager, Memory memory)
        {
            // Configure the form
            this.Text = "3D Renderer";
            this.Size = new System.Drawing.Size(800, 600);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            // Create a panel to host the rendering
            var renderPanel = new Panel
            {
                Dock = DockStyle.Fill
            };
            this.Controls.Add(renderPanel);

            // Initialize the Renderer3D
            _renderer3D = new Renderer3D(renderPanel, displayListManager, memory);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Dispose of the renderer when the form is closed
            _renderer3D.Dispose();
            base.OnFormClosing(e);
        }
    }
}
