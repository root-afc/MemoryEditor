
using Memory;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace Memory
{    
    public partial class Form1 : Form
    {

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern int VirtualQueryEx(IntPtr hProcess, IntPtr lpAddress, out MEMORY_BASIC_INFORMATION64 lpBuffer, uint dwLength);

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool ReadProcessMemory(IntPtr handle, IntPtr baseAddress, byte[] buffer, uint size, out int length);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool WriteProcessMemory(IntPtr handle, IntPtr baseAddress, byte[] buffer, int size, out int length);

        [DllImport("kernel32", SetLastError = true)]
        public static extern void GetSystemInfo(out SYSTEM_INFO lpSystemInfo);

        Dictionary<ulong, byte[]> AddressOf = new Dictionary<ulong, byte[]>();
        List<int> ListAddressIndex = new List<int>();
        List<int> ListAddressIndex2 = new List<int>();

        // flags
        const int PROCESS_WM_ALL = 0x001F0FFF;

        const int PROCESS_QUERY_INFORMATION = 0x0400;
        const int PROCESS_WM_READ = 0x0010;
        const int MEM_COMMIT = 0x00001000;
        const int PAGE_READWRITE = 0x04;

        public struct SYSTEM_INFO
        {
            public ushort processorArchitecture;
            ushort reserved;
            public uint pageSize;
            public IntPtr minimumApplicationAddress;  // minimum address
            public IntPtr maximumApplicationAddress;  // maximum address
            public IntPtr activeProcessorMask;
            public uint numberOfProcessors;
            public uint processorType;
            public uint allocationGranularity;
            public ushort processorLevel;
            public ushort processorRevision;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MEMORY_BASIC_INFORMATION64
        {
            public ulong BaseAddress;
            public ulong AllocationBase;
            public int AllocationProtect;
            public int __alignment1;
            public ulong RegionSize;
            public int State;
            public int Protect;
            public int Type;
            public int __alignment2;
        }

        Form2 form2 = new Form2();

        IntPtr processHandle;
        public Form1()
        {
            InitializeComponent();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            form2.ShowDialog();
            textBox1.Text = "Process: " + form2.ProcessName;
        }

        private void btnFirstScan_Click(object sender, EventArgs e)
        {
            CleanAddress();

            if (txtValue.Text.Length <= 0)
            {
                MessageBox.Show("Input value empty.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            byte[] data = BitConverter.GetBytes(Convert.ToInt32(txtValue.Text));

            processHandle = OpenProcess(PROCESS_WM_ALL, false, form2.Pid);

            firstSn(data);

            dataGridView1.ClearSelection();

            //byte[] buffer = BitConverter.GetBytes(1);
            //ReadMemory((IntPtr)48975756, buffer);

            //byte[] data = BitConverter.GetBytes(Convert.ToInt32(3500));
            //WriteMemory((IntPtr)48975756, data);
        }

        public void DumpMemoryRegions()
        {
            SYSTEM_INFO sys_info = new SYSTEM_INFO();
            GetSystemInfo(out sys_info);

            IntPtr proc_min_address = sys_info.minimumApplicationAddress;
            IntPtr proc_max_address = sys_info.maximumApplicationAddress;

            long proc_min_address_l = (long)proc_min_address;
            long proc_max_address_l = (long)proc_max_address;

            //ulong proc_min_address = 0x0;
            //ulong proc_max_address = 0xffffffff;

            MEMORY_BASIC_INFORMATION64 mem_basic_info = new MEMORY_BASIC_INFORMATION64();
            int bytesRead = 0;

            for (long i = proc_min_address_l; i < proc_max_address_l; i = i + (long)mem_basic_info.RegionSize)
            {
                VirtualQueryEx(processHandle, (IntPtr)i, out mem_basic_info, (uint)Marshal.SizeOf(mem_basic_info));

                if (mem_basic_info.Protect == PAGE_READWRITE && mem_basic_info.State == MEM_COMMIT)
                {
                    byte[] buffer = new byte[mem_basic_info.RegionSize];

                    // In case of memory protection or kernel level

                    if (!ReadProcessMemory(processHandle, (IntPtr)mem_basic_info.BaseAddress, buffer, (uint)mem_basic_info.RegionSize, out bytesRead))
                    {
                        throw new Exception("Error level kernel or restricted");
                    }
                    AddressOf.Add(mem_basic_info.BaseAddress, buffer);
                }
            }
        }

        public void firstSn(byte[] data)
        {
            DumpMemoryRegions();

            foreach (ulong Addr in AddressOf.Keys)
            {
                foreach (int i in MSearch.allIndexOf(AddressOf[Addr], data))
                {
                    // Address, actual, old
                    dataGridView1.Rows.Add(padAddress(IntPtr.Add((IntPtr)Addr, i).ToString("X")), BitConverter.ToInt32(data, 0), BitConverter.ToInt32(data, 0));
                }
            }
            timer1.Start();

        }
        public void UpdateValuesUp()
        {
            txtFound.Text = "Found: " + dataGridView1.Rows.Count.ToString();
            for (int i = 0; i < dataGridView1.Rows.Count; i++)
            {
                Int64 AddressUnit = Convert.ToInt64(dataGridView1.Rows[i].Cells[0].Value.ToString(), 16);
                byte[] buffer = BitConverter.GetBytes(1);
                dataGridView1.Rows[i].Cells[1].Value = ReadMemory((IntPtr)AddressUnit, buffer);
            }
        }

        public void UpdateValuesUDown()
        {
            for (int i = 0; i < dataGridView2.Rows.Count; i++)
            {
                Int64 AddressUnit = Convert.ToInt64(dataGridView2.Rows[i].Cells[0].Value.ToString(), 16);
                byte[] buffer = BitConverter.GetBytes(1);
                dataGridView2.Rows[i].Cells[1].Value = ReadMemory((IntPtr)AddressUnit, buffer);
            }
        }

        public int ReadMemory(IntPtr Address, byte[] buffer)
        {
            ReadProcessMemory(processHandle, Address, buffer, (uint)buffer.Length, out int bytesRead);
            return BitConverter.ToInt32(buffer, 0);
        }

        public void WriteMemory(IntPtr Address, byte[] data)
        {
            WriteProcessMemory(processHandle, Address, data, data.Length, out int bytesWritten);
            // Console.WriteLine(bytesWritten);
        }

        public string padAddress(string address)
        {
            if (address.Length == 8)
                return address;

            string ret = string.Empty;

            for (int i = address.Length; i < 8; i++)
                ret += "0";

            return ret + address;
        }
        private void CleanAddress()
        {
            AddressOf.Clear();
            dataGridView1.Rows.Clear();
            timer1.Stop();
        }
        private void timer1_Tick(object sender, EventArgs e)
        {
            UpdateValuesUp();
            UpdateValuesUDown();
        }

        private void addSelectedToListToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ListAddressIndex.Clear();

            foreach (DataGridViewRow r in dataGridView1.SelectedRows)
            {
                ListAddressIndex.Add(dataGridView1.Rows.IndexOf(r));
            }

            ListAddressIndex.Sort();

            foreach (var i in ListAddressIndex)
            {
                dataGridView2.Rows.Add(dataGridView1.Rows[i].Cells[0].Value, dataGridView1.Rows[i].Cells[1].Value);
            }
        }

        private void contextMenuStrip1_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // here check count datagridview_1 if 0 then no open;
            int count = dataGridView1.SelectedRows.Count;
            if (count <= 0)
            {
                e.Cancel = true;
            }
        }

        private void contextMenuStrip2_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // here check count datagridview_2 if 0 then no open;
            int count = dataGridView2.SelectedRows.Count;
            if (count <= 0)
            {
                e.Cancel = true;
            }
        }

        private void contextMenuStrip2_Click(object sender, EventArgs e)
        {
            Form3 form3 = new Form3();
            form3.ShowDialog();
            Int32 value = form3.value;

            foreach (DataGridViewRow item in dataGridView2.SelectedRows)
            {
                ListAddressIndex2.Add(item.Index);
            }

            ListAddressIndex2.Sort();

            foreach (int i in ListAddressIndex2)
            {
                // writing in memory...
                // Hexadecimal to Decimal
                IntPtr baseAdrInt = (IntPtr)Convert.ToInt64(dataGridView2.Rows[i].Cells[0].Value.ToString(), 16);
                byte[] data = BitConverter.GetBytes(value);
                WriteMemory(baseAdrInt, data);
            }
        }

        private void txtValue_KeyPress(object sender, KeyPressEventArgs e)
        {
           
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && (e.KeyChar != '.'))
            {
                e.Handled = true;
            }

            if ((e.KeyChar == '.') && ((sender as TextBox).Text.IndexOf('.') > -1))
            {
                e.Handled = true;
            }
        }
    }
}
