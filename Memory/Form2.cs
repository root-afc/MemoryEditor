using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
namespace Memory
{
    public partial class Form2 : Form
    {
        List<Proc> processes = new List<Proc>();
        private string processName;
        private int pid;
        public string ProcessName { get => processName; set => processName = value; }
        public int Pid { get => pid; set => pid = value; }

        public Form2()
        {
            InitializeComponent();
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            
            Process.GetProcesses().OrderBy(p => p.ProcessName).ToList().ForEach(p =>
            {
                processes.Add(new Proc { ProcessName = p.ProcessName, Id = p.Id });
            });

            dataGridView1.DataSource = processes;
        }

        private void button1_Click(object sender, EventArgs e)
        {
        }

        private void dataGridView1_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            string p = Convert.ToString(dataGridView1.SelectedRows[0].Cells["ProcessName"].Value);
            int id = Convert.ToInt32(dataGridView1.SelectedRows[0].Cells["Id"].Value);
            DialogResult = DialogResult.OK;
            ProcessName = p;
            Pid = id;

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            try
            {
                Regex rgx = new Regex(textBox1.Text, RegexOptions.IgnoreCase);
                dataGridView1.DataSource = processes.Where(x => rgx.IsMatch(x.ProcessName)).ToList();
            }
            catch (Exception)
            {

                throw;
            }
        }
    }

    public class Proc
    {
        public string ProcessName { get; set; }
        public int Id { get; set; }
    }
}
