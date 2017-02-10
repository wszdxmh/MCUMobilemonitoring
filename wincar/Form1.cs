using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.Ports;//必须加载串口的命名空间
using System.IO;//文件系统必须加载
using System.Drawing.Imaging;
using System.Threading;
//视频命名空间
using AForge;
using AForge.Video;
using AForge.Video.DirectShow;
using AForge.Imaging;
using AForge.Imaging.Filters;
using AForge.Controls;
using Size = System.Drawing.Size;
//二维码识别命名空间
using ZXing;
using ZXing.Presentation;
//图像保存命名空间
using System.Windows.Media.Imaging;
using System.Windows;

namespace wincar
{
    delegate void buttoncomser(object sender, EventArgs e);
    public partial class Form1 : Form
    {
        int MCU = 0, ReadMode = 0, picnum = 0;//定义文件标号
        private FilterInfoCollection videoDevices;
        private StringBuilder builder = new StringBuilder();//避免在事件处理方法中反复的创建，定义到外面。
        private long received_count = 0;//接收计数
        private long send_count = 0;//发送计数
        private bool Listening = false;//是否没有执行完invoke相关操作
        private bool Closing = false;//是否正在关闭串口，执行Application.DoEvents，并阻止再次invoke
        public Form1()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            string[] portsName = SerialPort.GetPortNames();//获取串口名称
            Array.Sort(portsName);//对串口名称进行排序
            comboBox1.Items.AddRange(portsName);//把名称添加到列表
            comboBox1.SelectedIndex = comboBox1.Items.Count > 0 ? 0 : -1;//把第一个名称选中
            comboBox2.SelectedIndex = comboBox2.Items.IndexOf("9600");//选中波特率9600
            serialPort1.DataReceived += port_DataRecerived;//添加事件，特别重要！！！
            //初始化状态参数
            toolStripStatusLabel1.Text = "串口号：" + comboBox1.Text + " |";
            toolStripStatusLabel2.Text = "波特率：" + comboBox2.Text + " |";
            toolStripStatusLabel4.Text = "Rx：" + received_count.ToString() + " |";
            toolStripStatusLabel5.Text = "Tx：" + send_count.ToString() + " |";
            toolStripStatusLabel6.Text = "拍照张数：" + picnum.ToString();
            //帮助选项初始化
            comboBox4.SelectedItem = comboBox4.Items[0];
            comboBox5.SelectedItem = comboBox5.Items[0];
            string[] test = File.ReadAllLines("Release/readme/read" + MCU + ReadMode + ".c");
            richTextBox1.Text = "";
            for (int i = 0; i < test.Length; i++)
                richTextBox1.Text += test[i] + "\n";
            toolStripStatusLabel3.Text = DateTime.Now.ToString();
            try
            {
                // 枚举所有视频输入设备
                videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);

                if (videoDevices.Count == 0)
                    throw new ApplicationException();
                foreach (FilterInfo device in videoDevices)
                {
                    comboBox3.Items.Add(device.Name);
                }
                comboBox3.SelectedIndex = 0;
            }
            catch (ApplicationException)
            {
                comboBox3.Items.Add("No local capture devices");
                videoDevices = null;
            }
        }

        /************************************关闭窗体事件**************************************/
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (buttonCamera.Text == "关闭视频")
            {
                buttonCamera.Text = "打开视频";
                videoSourcePlayer1.SignalToStop();
                videoSourcePlayer1.WaitForStop();
            }
        }

        /******************************串口接收事件******************************************/
        private void port_DataRecerived(object sender, SerialDataReceivedEventArgs e)
        {
            if (Closing) return;
            try
            {
                uint ultrasonic;
                //int n = serialPort1.BytesToRead; //记录下缓冲区的字节个数 
                byte[] buf = new byte[4]; //声明一个临时数组存储当前来的串口数据
                serialPort1.Read(buf, 0, 4); //读取缓冲数据到buf中，同时将这串数据从缓冲区移除 
                //string str = string.Concat(buf[1]);//转换为字符串型
                builder.Clear();
                received_count++;
                toolStripStatusLabel4.Text = "Rx:" + received_count.ToString() + " |";
                if ((buf[0] == 0x00) && (buf[3] == 0xff))
                {
                    ultrasonic = buf[1];
                    ultrasonic = ultrasonic << 8;
                    ultrasonic = ultrasonic | buf[2];
                    //this.Invoke((EventHandler)(delegate
                    //{
                    //    textBox1.Text = ultrasonic.ToString();//长度为1补0
                    //}));
                    this.textBox1.Invoke(new MethodInvoker(delegate
                    {
                        this.textBox1.Text = ultrasonic.ToString();//长度为1补0
                    }));
                }
                else
                {
                    //MessageBox.Show("数据接收失败，请检查！", "提示");
                    if (button9.Text == "关闭串口")
                        serialPort1.DiscardInBuffer();//清空接收缓存区
                }
            }
            finally { Listening = false; }
        }
        /**************************************扫描串口***************************************/
        private void SearchandAddSerialPort(SerialPort Myport, ComboBox Mybox)
        {
            int[] Mycom = new int[20];
            string buffer;
            int count = 0;
            Mybox.Items.Clear();//清空combox里的元素
            for (int i = 1; i < 20; i++)
            {
                try
                {
                    buffer = "COM" + i.ToString();
                    Myport.PortName = buffer;
                    Myport.Open();//打开串口
                    Mycom[i] = i;
                    Mybox.Items.Add(buffer);//把串口名称增加到下拉元素中
                    Myport.Close();//关闭串口
                    count++;
                }
                catch
                { }
            }
            Mybox.Text = "COM" + Mycom[0].ToString();
        }
        /*******************************打开摄像头****************************************/
        private void CameraConn()
        {
            // create first video source
            VideoCaptureDevice videoSource1 = new VideoCaptureDevice(videoDevices[comboBox3.SelectedIndex].MonikerString);
            //videoSource1.DesiredFrameSize = new Size(320, 240);
            //videoSource1.DesiredFrameRate = 120;
            videoSourcePlayer1.VideoSource = videoSource1;
            videoSourcePlayer1.Start();
        }
        /***********************************串口开关按钮*************************************/
        private void button_com_Click(object sender, EventArgs e)
        {
            if (comboBox1.Text == "")
            {
                MessageBox.Show("请检查串口是接入或串口驱动动是否完好", "提示");
            }
            else
            {
                if (!serialPort1.IsOpen)//判断串口是否打开
                {
                    button_com.Text = "关闭串口";
                    try
                    {
                        serialPort1.PortName = comboBox1.Text;//读取串口号
                        serialPort1.BaudRate = Convert.ToInt32(comboBox2.Text, 10);//设置波特率，转换成int型
                        serialPort1.Open();//打开串口                       
                        button17.PerformClick();
                    }
                    catch (Exception er)
                    {
                        if (serialPort1.IsOpen)//是否打开了串口
                            serialPort1.Close();//关闭串口
                        MessageBox.Show(er.ToString(), "提示");//提示出错原因
                    }
                }
                else //串口打开
                {
                    button_com.Text = "打开串口";
                    try
                    {
                        Closing = true;
                        while (Listening) Application.DoEvents();
                        serialPort1.Close();//关闭串口
                        Closing = false;
                    }
                    catch (Exception er)
                    {
                        MessageBox.Show(er.ToString(), "提示");
                    }
                }
            }
        }

        /**********************************串口扫描按钮***************************************/
        private void button_search_Click(object sender, EventArgs e)
        {
            if (!serialPort1.IsOpen)
            {
                if (comboBox1.Text == "")
                {
                    string[] portsName = SerialPort.GetPortNames();//获取串口名称
                    Array.Sort(portsName);//对串口名称进行排序
                    comboBox1.Items.AddRange(portsName);//把名称添加到列表
                    comboBox1.SelectedIndex = comboBox1.Items.Count > 0 ? 0 : -1;//把第一个名称选中
                }
            }
            else
            {
                MessageBox.Show("请先关闭串口，再进行扫描！", "提示");
            }
        }

        /**********************************串口发送协议***************************************/
        public void WriteByteToSerialPort(byte hearder, byte data)
        {
            byte[] Buffer = new byte[3] { hearder, data, hearder };
            Buffer[2] = (byte)~hearder;
            if (serialPort1.IsOpen)
            {
                try
                {
                    serialPort1.Write(Buffer, 0, 3);
                    send_count++;
                    toolStripStatusLabel5.Text = "Tx：" + send_count.ToString() + " |";
                    if (button9.Text == "关闭串口")
                    {
                        send_count++;
                        toolStripStatusLabel5.Text = "Tx：" + send_count.ToString() + " |";
                    }
                }
                catch
                {
                    MessageBox.Show("发送错误，请检查！", "提示");
                }
            }
        }
        /***********************************定时器1S**************************************/
        private void timer1_Tick(object sender, EventArgs e)
        {
            toolStripStatusLabel1.Text = "串口号：" + comboBox1.Text + " |";
            toolStripStatusLabel2.Text = "波特率：" + comboBox2.Text + " |";
            toolStripStatusLabel3.Text = DateTime.Now.ToString() + " |";
            //WriteByteToSerialPort(0x00,0xff);
        }
        /************************************视频开关**************************************/
        private void buttonCamera_Click(object sender, EventArgs e)
        {
            if (buttonCamera.Text == "打开视频")
            {
                buttonCamera.Text = "关闭视频";
                CameraConn();
            }
            else
            {
                buttonCamera.Text = "打开视频";
                videoSourcePlayer1.SignalToStop();
                videoSourcePlayer1.WaitForStop();
            }
        }
        /******************************帮助面板功能选项***************************************/
        private void comboBox4_SelectedIndexChanged(object sender, EventArgs e)
        {
            ReadMode = comboBox4.SelectedIndex;
            richTextBox1.Text = "";
            string[] text = File.ReadAllLines("Release/readme/read" + MCU + ReadMode + ".c");
            for (int i = 0; i < text.Length; i++)
                richTextBox1.Text += text[i] + "\n";
        }
        /******************************帮助面板芯片型号选项************************************/
        private void comboBox5_SelectedIndexChanged(object sender, EventArgs e)
        {
            MCU = comboBox5.SelectedIndex;
            richTextBox1.Text = "";
            string[] text = File.ReadAllLines("Release/readme/read" + MCU + ReadMode + ".c");
            for (int i = 0; i < text.Length; i++)
                richTextBox1.Text += text[i] + "\n";
        }
        /*******************************键盘事件****************************************/
        private void Form1_KeyPress(object sender, KeyPressEventArgs e)
        {
            switch (e.KeyChar)
            {
                case 'W': case 'w': button4.PerformClick();break;//按下W键
                case 'S': case 's': button5.PerformClick();break;//按下S键
                case 'A': case 'a': button6.PerformClick(); break;//按下A键
                case 'D': case 'd': button7.PerformClick(); break;//按下D键
                case 'U': case 'u': button10.PerformClick(); break;//按下U键
                case 'J': case 'j': button11.PerformClick(); break;//按下J键
                case 'I': case 'i': button12.PerformClick(); break;//按下I键
                case 'K': case 'k': button13.PerformClick(); break;//按下K键
                case 'O': case 'o': button1.PerformClick(); break;//按下O键
                case 'L': case 'l': button2.PerformClick(); break;//按下L键
                case 'p': case 'P': button8.PerformClick(); break;//按下P键
                case ';': case ':': button9.PerformClick(); break;//按下;键
                case '1': case '!': button3.PerformClick(); break;//按下1键
                case '2': case '@': button14.PerformClick(); break;//按下2键
                case '3': case '#': button15.PerformClick(); break;//按下3键
                case '4': case '$': button16.PerformClick(); break;//按下4键
                case 'Z': case 'z': button17.PerformClick(); break;//按下Z键
            }
        }
        /*******************************单击控件拍照***************************************/
        private void videoSourcePlayer1_MouseClick(object sender, MouseEventArgs e)
        {
            //try
            //{
            //    int number = 0;
            //    number++;
            //    string fileImageName = "Release" + "-" + number + ".bmp";
            //    string fileCapturePath = g_s_AutoSavePath + "c:\\" + "\\";
            //    if (!Directory.Exists(fileCapturePath))
            //        Directory.CreateDirectory(fileCapturePath);

            //    //抓到图保存到指定路径
            //    Bitmap bmp = null;
            //    //bmp = videoSourcePlayer1.GetCurrentVideoFrame();
            //    if (bmp == null)
            //    {
            //        MessageBox.Show("捕获图像失败！", "提示");
            //        return;
            //    }

            //    bmp.Save(fileCapturePath + fileImageName, ImageFormat.Bmp);

            //}
            //catch (Exception ex)
            //{
            //    MessageBox.Show("捕获图像失败！" + ex.Message, "提示");
            //}
            try
            {
                if (videoSourcePlayer1.IsRunning)
                {
                    BitmapSource bitmapSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                                    videoSourcePlayer1.GetCurrentVideoFrame().GetHbitmap(),
                                    IntPtr.Zero,
                                     Int32Rect.Empty,
                                    BitmapSizeOptions.FromEmptyOptions());
                    PngBitmapEncoder pE = new PngBitmapEncoder();
                    pE.Frames.Add(BitmapFrame.Create(bitmapSource));
                    string picName = GetImagePath() + "\\" + DateTime.Now.ToString("yyyyMMddhhmmss") + picnum + ".jpg";
                    picnum++;
                    toolStripStatusLabel6.Text = "拍照张数：" + picnum.ToString();
                    if (File.Exists(picName))
                    {
                        File.Delete(picName);
                    }
                    using (Stream stream = File.Create(picName))
                    {
                        pE.Save(stream);
                    }
                    //拍照完成后关摄像头并刷新同时关窗体
                    //if (videoSourcePlayer1 != null && videoSourcePlayer1.IsRunning)
                    //{
                    //    videoSourcePlayer1.SignalToStop();
                    //    videoSourcePlayer1.WaitForStop();
                    //}

                    //this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("摄像头异常：" + ex.Message);
            }
        }

        private string GetImagePath()
        {
            string personImgPath = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory)
                         + Path.DirectorySeparatorChar.ToString() + "PersonImg";
            if (!Directory.Exists(personImgPath))
            {
                Directory.CreateDirectory(personImgPath);
            }
            return personImgPath;
        }

        /*********************************按钮事件******************************************/
        private void button4_Click(object sender, EventArgs e)
        {
            WriteByteToSerialPort(0x04,0x01);//前进
        }

        private void button5_Click(object sender, EventArgs e)
        {
            WriteByteToSerialPort(0x04, 0x02);//后退
        }

        private void button6_Click(object sender, EventArgs e)
        {
            WriteByteToSerialPort(0x04, 0x03);//左转
        }

        private void button7_Click(object sender, EventArgs e)
        {
            WriteByteToSerialPort(0x04, 0x04);//右转
        }

        private void button10_Click(object sender, EventArgs e)
        {
            WriteByteToSerialPort(0x00, 0x01);//肩+
        }

        private void button11_Click(object sender, EventArgs e)
        {
            WriteByteToSerialPort(0x00, 0x02);//肩-
        }

        private void button12_Click(object sender, EventArgs e)
        {
            WriteByteToSerialPort(0x01, 0x01);//肘+
        }

        private void button13_Click(object sender, EventArgs e)
        {
            WriteByteToSerialPort(0x01, 0x02);//肘-
        }

        private void button1_Click(object sender, EventArgs e)
        {
            WriteByteToSerialPort(0x02, 0x01);//腕+
        }

        private void button2_Click(object sender, EventArgs e)
        {
            WriteByteToSerialPort(0x02, 0x02);//腕-
        }

        private void button3_Click(object sender, EventArgs e)
        {
            WriteByteToSerialPort(0x05, 0x01);//举高
        }

        private void button14_Click(object sender, EventArgs e)
        {
            WriteByteToSerialPort(0x05, 0x02);//抓上
        }

        private void button15_Click(object sender, EventArgs e)
        {
            WriteByteToSerialPort(0x05, 0x03);//抓中
        }

        private void button16_Click(object sender, EventArgs e)
        {
            WriteByteToSerialPort(0x05, 0x04);//抓下
        }

        private void button17_Click(object sender, EventArgs e)
        {
            WriteByteToSerialPort(0x04, 0x05);//停止
        }

        private void button8_Click(object sender, EventArgs e)
        {
            WriteByteToSerialPort(0x05, 0x07);//抓
        }

        private void button9_Click(object sender, EventArgs e)
        {
            WriteByteToSerialPort(0x05, 0x08);//放
        }
    }
}
