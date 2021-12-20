using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Memory
{
    public partial class Form3 : Form
    {
        public Int32 value { get; set; }
        public Form3()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {

            if (txtValue.Text.Length <= 0)
            {
                MessageBox.Show("Input value empty.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            value = Convert.ToInt32(txtValue.Text);
            Close();
        }
    }
}
