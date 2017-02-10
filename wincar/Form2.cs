using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using wincar;
using System.IO.Ports;//必须加载串口的命名空间
using System.IO;//文件系统必须加载

namespace wincar
{
    public partial class Form2 : Form
    {
        Form1 frm1 = new Form1();
        public Form2()
        {
            InitializeComponent();
        }
        
        private void Form2_Load(object sender, EventArgs e)
        {
            
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //Form1 frm1 = new Form1();
            //frm1.WriteByteToSerialPort(0x00,0x01);

            //SendSer send2 = new SendSer(frm1.WriteByteToSerialPort);
            //send2(0x00,0x01);

            //Form1 frm1 = (Form1)this.Owner;
            //frm1.WriteByteToSerialPort(0x00, 0x01);
        }

        private void button2_Click(object sender, EventArgs e)
        {

        }
    }
}
