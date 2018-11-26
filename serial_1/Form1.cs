using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO.Ports;
using System.IO;

namespace serial_1
{
    public partial class Form1 : Form
    {
        private bool open = false;  //波形开关标志
        private byte NUM = 0;   //曲线数目标志
        private bool isover = false;//曲线数目稳定标志
        private bool[] isopen = new bool[8] { false, false, false, false, false, false, false, false };//各曲线开关标志
        private float time = 0;//模拟时间
    //    private float ratio_X = 1;//X轴比率
        private float ratio_Y = 1;  //Y轴比率
        private float Unit_length = 50;//单位格大小
        private float DrawStep = 1;//默认绘制单位
        private float Y_Max = 500;//Y轴最大数值
        private float Y_MID = 200;//
        private float Y_Min = 0;//Y轴最小数值
        private const float MaxStep = 50;//绘制单位最大值
        private const float MinStep = 0.01f;//绘制单位最小值
        private const int StartPrint = 30;//点坐标偏移量
        private List<float>[] DataList = new List<float>[8] {
            new List<float>(),new List<float>(),new List<float>(),new List<float>(),
            new List<float>(),new List<float>(),new List<float>(),new List<float>() };//数据结构----线性链表
        private Pen TablePen = new Pen(Color.FromArgb(0xDD, 0x00, 0x00));//轴线颜色
        private Pen[] LinesPen = new Pen[8] { new Pen(Color.FromArgb(0x00, 0x00, 0xFF)),
            new Pen(Color.FromArgb(0x00, 0xFF, 0x00)), new Pen(Color.FromArgb(0x00, 0xFF, 0xFF)),
            new Pen(Color.FromArgb(0xFF, 0x00, 0xFF)), new Pen(Color.FromArgb(0xD0, 0xA0, 0x6F)),
            new Pen(Color.FromArgb(0xFF, 0x64, 0x96)), new Pen(Color.FromArgb(0xEE, 0xC7, 0x10)),
            new Pen(Color.FromArgb(0xFF, 0xFF, 0xFF))};//蓝，绿，蓝绿，紫，棕，粉，黄，白

        public Form1()
        {
            this.SetStyle(ControlStyles.DoubleBuffer | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true);//开启双缓冲
            this.UpdateStyles();
            InitializeComponent();
            serialPort1.Encoding = Encoding.GetEncoding("GB2312"); //GB2312编码 支持中文
            Control.CheckForIllegalCrossThreadCalls = false;
        }

        private void AddData(float[] Data,byte num)//添加数据
        {
            for (int i = 0; i < Data.Length; i++)
                DataList[num].Add(Data[i]);//链表尾部添加数据
        }

        private void picturebox_init()
        {
            NUM = 0;   //曲线数目标志
            isover = false;//曲线数目稳定标志
            for(int i=0;i<8;i++)
            {
                isopen[i] = false;
            }
            time = 0;//模拟时间
            ratio_Y = 1;  //Y轴比率
            Unit_length = 50;//单位格大小
             DrawStep = 1;//默认绘制单位
            Y_Max = 500;//Y轴最大数值
            Y_MID = 200;//
            Y_Min = 0;//Y轴最小数值
        }

        private void pictureBox1_Paint(object sender, PaintEventArgs e)//pictureBox1重画事件处理函数
        {
            label4.BackColor = Color.Black;//默认全黑
            label5.BackColor = Color.Black;
            label6.BackColor = Color.Black;
            label7.BackColor = Color.Black;
            label8.BackColor = Color.Black;
            label9.BackColor = Color.Black;
            label10.BackColor = Color.Black;
            label11.BackColor = Color.Black;
            for (int j = 0; j <= NUM; j++)//显示曲线所用颜色
            {
                switch (j)
                {
                    case 0: if (isopen[0] == false) break;
                        if (DataList[0].Count > 0)
                        { 
                            label4.BackColor = Color.FromArgb(0x00, 0x00, 0xFF);
                        }  
                        break;
                    case 1: if (isopen[1] == false) break;
                        label5.BackColor = Color.FromArgb(0x00, 0xFF, 0x00); break;
                    case 2: if (isopen[2] == false) break;
                        label6.BackColor = Color.FromArgb(0x00, 0xFF, 0xFF); break;
                    case 3: if (isopen[3] == false) break;
                        label7.BackColor = Color.FromArgb(0xFF, 0x00, 0xFF); break;
                    case 4: if (isopen[4] == false) break;
                        label8.BackColor = Color.FromArgb(0xD0, 0xA0, 0x6F); break;
                    case 5: if (isopen[5] == false) break; 
                        label9.BackColor = Color.FromArgb(0xFF, 0x64, 0x96); break;
                    case 6: if (isopen[6] == false) break; 
                        label10.BackColor = Color.FromArgb(0xEE, 0xC7, 0x10); break;
                    case 7: if (isopen[7] == false) break; 
                        label11.BackColor = Color.FromArgb(0xFF, 0xFF, 0xFF); break;
                    default: break;
                }
            }

            String Str = "";
            System.Drawing.Drawing2D.GraphicsPath gp = new System.Drawing.Drawing2D.GraphicsPath();
            e.Graphics.FillRectangle(Brushes.Black, e.Graphics.ClipBounds);

           for (int i = 0; i <= NUM; i++)
            {
                try
                {
                    if (DataList[i].Count - 1 >= (int)((pictureBox1.ClientRectangle.Width - StartPrint) / 1.5) / DrawStep)//如果数据量大于可容纳的数据量，即删除最左数据
                    {
                        time += 1 / (DrawStep * 50) / (NUM + 1);
                        DataList[i].RemoveRange(0, DataList[i].Count - (int)(((pictureBox1.ClientRectangle.Width - StartPrint) / 1.5) / DrawStep) - 1);
                    }
                }
                catch { }
            }

            //改变一些参数
            try
            {
                for (int i = 0; i <= NUM; i++)
                {
                    if (isopen[i] == true)
                    {
                        Y_Max = DataList[i].Max();
                        Y_Min = DataList[i].Min();    //取出链表当中最小值
                        break;
                    }
                    else
                    {
                        Y_Max = 500;//Y轴最大数值
                        Y_Min = 0;//Y轴最小数值
                    }
                }

                for (int i = 0; i <= NUM; i++)
                {
                    if (DataList[i].Count > 0 && isopen[i] == true)
                    {
                        Y_Max = Math.Max(Y_Max, DataList[i].Max());
                        Y_Min = Math.Min(Y_Min, DataList[i].Min());
                    }
                }    
            }
            catch { }
            float err1 = Y_Max - Y_Min;
            if (err1 <= 10)
            {
                err1 = 10;
                Y_MID = (Y_Max + Y_Min) / 2;
                Y_Max = Y_MID + 5;
                Y_Min = Y_MID - 5;
            }

            ratio_Y = (pictureBox1.ClientRectangle.Height - 2 * StartPrint) / (Y_Max - Y_Min);//求得Y轴比例
            Unit_length = (Y_Max - Y_Min)/10;//默认格数为10，求每个格的高度
            float length = Unit_length;
            int dis = 1;
            while (length > 1)//求出当前每个格的合适量度
            {
                if (length / 10 >= 0.5)//10
                {
                    dis *= 10;
                }
                else if (length / 5 >= 0.5)//5
                {
                    dis *= 5;
                }
                else if (length / 2 >= 0.5)//2
                {
                    dis *= 2;
                }   
                length /= 10;
            }
            int min = 0;//Y轴最小的可以整除单位格量度的最小值
            if (Y_Min > 0)//如果数据最小值是正数，那就将数据整除单位格量度后+1再乘以单位格量度
            { 
                min = ((int)Y_Min / dis + 1) * dis;
            }
            else//如果数据最小值不是正数，那就将数据取负后整除单位格量度后再乘以单位格量度再取负
            {
                min = ((int)(-Y_Min) / dis) * dis;
                min = -min;
            }
            Unit_length = (Y_Max - Y_Min) / ((Y_Max - Y_Min) / dis);//重新计算单位格高度

            
            //Draw X 横向轴绘制
            for (int i = 0; i <= pictureBox1.ClientRectangle.Width / 50; i++)
            {
                e.Graphics.DrawLine(TablePen, StartPrint + i * 50, StartPrint, StartPrint + i * 50, pictureBox1.ClientRectangle.Height - StartPrint);//画线
              //   gp.AddString(((int)((i) * (50 / DrawStep) + time)).ToString(), pictureBox1.Font.FontFamily, (int)FontStyle.Regular, 12, new RectangleF(StartPrint + i * 50 - 7, pictureBox1.ClientRectangle.Height - StartPrint + 4, 400, 50), null);//添加文字
               gp.AddString(((int)((i + time) * (50 / DrawStep))).ToString(), pictureBox1.Font.FontFamily, (int)FontStyle.Regular, 12, new RectangleF(StartPrint + i * 50 - 7, pictureBox1.ClientRectangle.Height - StartPrint + 4, 400, 50), null);//添加文字
            }

            //Draw Y 纵向轴绘制
            for (int i = 0; i <= (Y_Max - min) / Unit_length; i++)
            {
                e.Graphics.DrawLine(TablePen, StartPrint, StartPrint, pictureBox1.ClientRectangle.Width, StartPrint);//画线
                e.Graphics.DrawLine(TablePen, StartPrint, (StartPrint + (Y_Max - Y_Min) * ratio_Y), pictureBox1.ClientRectangle.Width, (StartPrint + (Y_Max - Y_Min) * ratio_Y));//画线

                e.Graphics.DrawLine(TablePen, StartPrint, (StartPrint + (Y_Max - min - i * Unit_length) * ratio_Y), pictureBox1.ClientRectangle.Width, (StartPrint + (Y_Max - min - i * Unit_length) * ratio_Y));//画线
                try
                {
                    Str = (min + i * dis).ToString();//计算Y轴坐标
                }
                catch { }
                gp.AddString(Str, pictureBox1.Font.FontFamily, (int)FontStyle.Regular, 12, new RectangleF(0, (StartPrint + (Y_Max - min - i * Unit_length) * ratio_Y) - 8, 400, 50), null);//添加文字

            }
            e.Graphics.DrawPath(TablePen, gp);//写文字
            for (int j = 0; j <= NUM; j++)
            {
                try
                {
                    if (isopen[j] == true)
                    {
                        for (int i = 0; i < DataList[j].Count - 1; i++)//绘制
                        {
                            e.Graphics.DrawLine(LinesPen[j], StartPrint + i * DrawStep, pictureBox1.ClientRectangle.Height - StartPrint - (DataList[j][i] - Y_Min) * ratio_Y, StartPrint + (i + 1) * DrawStep, pictureBox1.ClientRectangle.Height - StartPrint - (DataList[j][i + 1] - Y_Min) * ratio_Y);
                        }
                    }
                }
                catch { }  
            }
            
        }

        private void button4_Click(object sender, EventArgs e)//波形显示按钮
        {
            if (open == false)//第一次创建 open = true
            {
                pictureBox1.Visible = true;
                open = true;
                DrawStep = 1;
                label3.Visible = true;
                label4.Visible = true;
                label5.Visible = true;
                label6.Visible = true;
                label7.Visible = true;
                label8.Visible = true;
                label9.Visible = true;
                label10.Visible = true;
                label11.Visible = true;
                label12.Visible = false;
                button4.Text = "关闭波形";

            }
            else if(open == true)//关闭 open = false
            {
                time = 0;
                NUM = 0;
                for (int i = 0; i <= 7; i++)
                {
                    DataList[i].Clear();//清除链表数据            
                }
                pictureBox1.Invalidate();//重画pictureBox1
                pictureBox1.Visible = false;
                open = false;
                isover = false;
                DrawStep = 1;
                label3.Visible = false;
                label4.Visible = false;
                label5.Visible = false;
                label6.Visible = false;
                label7.Visible = false;
                label8.Visible = false;
                label9.Visible = false;
                label10.Visible = false;
                label11.Visible = false;
                label12.Visible = true;
                button4.Text = "波形显示";

            }
 
        }

        private void Form1_Load(object sender, EventArgs e)//窗体初始化
        {
            pictureBox1.Visible = false;
            label3.Visible = false;
            label4.Visible = false;
            label5.Visible = false;
            label6.Visible = false;
            label7.Visible = false;
            label8.Visible = false;
            label9.Visible = false;
            label10.Visible = false;
            label11.Visible = false;
            
            comboBox1.Enabled = false;


            if (Settings1.Default.b != "")
            {
                if (Settings1.Default.b == "4800" || Settings1.Default.b == "9600" || Settings1.Default.b == "38400" || Settings1.Default.b == "115200")
                {
                    comboBox2.Text = Settings1.Default.b;
                }
                else 
                {
                    comboBox2.DropDownStyle = ComboBoxStyle.DropDown;
                    comboBox2.Text = Settings1.Default.b;
                }    
            }
            else 
            {
                comboBox2.Text = "9600";//设置默认波特率9600
            }
            serialPort1.DataReceived += new SerialDataReceivedEventHandler(port_DataReceived);//必须手动添加串口接收事件处理程序
            this.MouseWheel += Form1_MouseWheel;
            string[] ports = SerialPort.GetPortNames();//获取当前计算机的串口号
            for (int i = 0; i < ports.Length; i++)//将串口号添加到comboBox1下拉列表中
            {
                comboBox1.Items.Add(ports[i]);
            }
            if (ports.Length != 0)//判断当前计算机端口数是否为空
            {
                if (comboBox1.Items.Contains(Settings1.Default.a))//判断上一次使用的串口号还在不在
                {
                    comboBox1.Text = Settings1.Default.a;
                }
                else
                {
                    comboBox1.Text = ports[0];//将串口号最小的添加到Text中
                }
            }
            try
            {
                //try
                //{
                //    serialPort1.Close();//关闭串口
                //}
                //catch { }
                serialPort1.PortName = comboBox1.Text;//设置串口号
                serialPort1.BaudRate = Convert.ToInt32(comboBox2.Text);//设置串口波特率
                serialPort1.Open();//打开串口
                button2.Text = "关闭串口";
            }
            catch
            {
                if (ports.Length == 0)
                    MessageBox.Show("没有搜索到串口", "提示");
                else
                    MessageBox.Show("串口打开失败！", "错误");
            }
            comboBox1.Enabled = true;
        }

        void Form1_MouseWheel(object sender, MouseEventArgs e)//鼠标滚轮事件
        {
            int x = Control.MousePosition.X;//记录鼠标的位置
            int y = Control.MousePosition.Y;

            if (open == true)
            {
                if (e.Delta > 0) //放大图片
                {
                    if (DrawStep < MaxStep)//绘制单位递增
                        DrawStep += DrawStep * 0.1f;
                    pictureBox1.Invalidate();//重画pictureBox1
                }
                else //缩小图片
                {
                    if (DrawStep > MinStep)//绘制单位递减
                        DrawStep -= DrawStep * 0.1f;
                    pictureBox1.Invalidate();//重画pictureBox1
                }
            }
            
         }

        private void port_DataReceived(object sender, SerialDataReceivedEventArgs e)//串口数据接收事件
        {
            if (!checkBox1.Checked)//如果接收模式为字符模式
            {
                { //字符模式下，可以使用波形显示
                    button4.Enabled = true;
                }

                string str ="";//读到
               // string str = serialPort1.ReadExisting();//字符串方式读
               // string str = serialPort1.ReadLine();//此法效果不好
                if (open == true)
                {
                    try
                    {
                        //str = serialPort1.ReadExisting();//字符串方式读
                        str = serialPort1.ReadTo("\r\n");//读到“\r\n”
                    }
                    catch { }
                    str = str + "\r\n";

                    char[] buffer = str.ToCharArray();//将字符串转换成字符数组，按字符进行数据处理
                    int[] temp_Integer = new int[8] { 0, 0, 0, 0, 0, 0, 0, 0 };//中间数据转换用
                    int[] temp_Decimal = new int[8] { 0, 0, 0, 0, 0, 0, 0, 0 };//小数部分
                    int[] flag_Minus = new int[8] { 1, 1, 1, 1, 1, 1, 1, 1 };//正负标志位
                    byte flag_End = 0;//回车符标志位
                    byte num = 0;//每行数据个数0,1,2,3,4,5,6,7
                    byte flag_Float = 0;//浮点型数据标志位
                    string str1 = "";//
                    foreach (char member in buffer)//串口数据处理
                    {
                     
                        if (flag_End == 0)
                        {
                            if (member == ' ' || member == '\t')
                            {
                                num++;
                                flag_Float = 0;
                            }
                            else if (member == '-')
                            {
                                flag_Minus[num] = -1;
                            }
                            else if (member == '\r')
                            {
                                flag_End = 1;
                            }
                            else if (member == '\n')
                            {
                                //     flag_end = 1;
                            }
                            else if (member == '.')
                            {
                                flag_Float = 1;
                            }
                            else if (member >= '0' && member <= '9')
                            {
                                str1 = member.ToString();
                                if (flag_Float == 0)
                                { 
                                    try
                                    {
                                        temp_Integer[num] = (int)(temp_Integer[num] * 10 + (Convert.ToByte(str1, 10)));
                                    }
                                    catch { }
                                }
                                else if (flag_Float == 1)
                                {
                                    try
                                    {
                                        temp_Decimal[num] = (int)(temp_Decimal[num] * 10 + (Convert.ToByte(str1, 10)));
                                    }
                                    catch { }
                                }
                                
                            }
                        }
                        else if (flag_End == 1)
                        {
                            if (member == '\n')
                            {

                                byte temp = NUM;
                                NUM = num;
                                if (temp == NUM)
                                {
                                    if (isover == false)
                                    {
                                        for (int i = 0; i <= NUM; i++)
                                        {
                                            isopen[i] = true;
                                        }
                                        pictureBox1.Invalidate();//重画pictureBox1
                                    }
                                    isover = true;
                                }
                                else 
                                {
                                    isover = false;
                                    pictureBox1.Invalidate();//重画pictureBox1
                                }

                                for (byte i = 0; i <= num; i++)
                                {
                                    float[] data = new float[1];
                                    float dec = temp_Decimal[i];
                                    while (dec >= 1)
                                    {
                                        dec /= 10;
                                    }
                                    data[0] = (temp_Integer[i] + dec) * flag_Minus[i];
                                    AddData(data,i);//向链表追加数据
                                    flag_Minus[i] = 1;
                                    temp_Integer[i] = 0;
                                    temp_Decimal[i] = 0;
                                }
                                num = 0;
                                flag_End = 0;
                                flag_Float = 0;
                                str1 = "";
                                pictureBox1.Invalidate(true);//重画pictureBox1
                            }
                            else
                            {
                                for (byte i = 0; i <= num; i++)
                                {
                                    temp_Decimal[i] = 0;
                                    temp_Integer[i] = 0;
                                    flag_Minus[i] = 1;
                                }
                                flag_End = 0;
                                flag_Float = 0;
                                num = 0;
                                str1 = "";
                            }
                            
                        }
                       
                    }
                    pictureBox1.Invalidate(true);//重画pictureBox1 
                }
                else
                {
                    try
                    {
                        str = serialPort1.ReadExisting();//字符串方式读
                    }
                    catch { }
                }
                try { textBox1.AppendText(str); }
                catch { }
                
            }
            else//如果接收模式为数值接收,十六进制显示
            {
                { //十六进制下，禁止使用波形显示
                    pictureBox1.Visible = false;
                    open = false;
                    button4.Text = "波形显示";
                    button4.Enabled = false;
                    label3.Visible = false;
                    label4.Visible = false;
                    label5.Visible = false;
                    label6.Visible = false;
                    label7.Visible = false;
                    label8.Visible = false;
                    label9.Visible = false;
                    label10.Visible = false;
                    label11.Visible = false;

                } 
                byte[] buffer = System.Text.Encoding.Default.GetBytes(serialPort1.ReadExisting());//读入缓冲区所有字节（string形式）转换成byte类型的数组
                string str = "";
                foreach (byte member in buffer)
                {
                    string temp_str = Convert.ToString(member, 16).ToUpper();//转换为大写十六进制字符串
                    str += (temp_str.Length == 1 ? "0" : "") + temp_str + " ";
                }
                textBox1.AppendText(str);
            }
        }

        private void comboBox1_Click(object sender, EventArgs e)//串口号下拉列表单击事件处理函数
        {
            try
            {
                serialPort1.Close();//关闭串口
                button2.Text = "打开串口";
            }
            catch { }
            
            string[] ports = SerialPort.GetPortNames();//获取当前计算机的串口号
            string last_text = comboBox1.Text;//记录上一次使用的串口号
            comboBox1.Text = "";//清空text
            comboBox1.Items.Clear();//清空comboBox1下拉列表中的串口号
            for (int i = 0; i < ports.Length; i++)//向comboBox1下拉列表中添加串口号
            {
                comboBox1.Items.Add(ports[i]);
            }
            if (ports.Length != 0)//判断当前计算机端口数是否为空的话
            {
                if (comboBox1.Items.Contains(last_text))//判断上一次使用的串口号还在不在
                {
                    comboBox1.Text = last_text;
                }
                else//将串口号最小的添加到Text中
                { 
                    comboBox1.Text = ports[0];
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)//打开串口
        {
            if (button2.Text == "关闭串口")
            {
                try
                {
                    //string str = serialPort1.ReadExisting();//字符串方式读
                    serialPort1.Close();//关闭串口
                }
                catch { }
                button2.Text = "打开串口";
            }
            else   //button2.Text如果是"打开串口"
            {
                try
                {
                 
                    serialPort1.PortName = comboBox1.Text;//设置串口号
                    serialPort1.BaudRate = Convert.ToInt32(comboBox2.Text);//设置波特率
                    serialPort1.Open();//打开串口
                    button2.Text = "关闭串口";
                }
                catch
                {
                    MessageBox.Show("串口打开失败！", "错误");
                }
            }
        }

        private void button3_Click(object sender, EventArgs e)//清除接收
        {
            textBox1.Text = "";//清空接收
            if (open == true)//如果波形显示打开了
            {
                for (int i = 0; i <= 7; i++ )
                    DataList[i].Clear();//清除链表数据
                picturebox_init();
                pictureBox1.Invalidate();//重画pictureBox1
            }
        }

        private void button1_Click(object sender, EventArgs e)//数据发送
        {
            if (serialPort1.IsOpen)
            {
                try
                {
                    serialPort1.Write(textBox2.Text);//向串口写字符串
                }
                catch
                {
                    MessageBox.Show("串口发送失败！", "错误");
                }
            }
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)//波特率被重新设置处理函数
        {
            if (comboBox2.Text == "自定义")
            {
                comboBox2.Text = "";
                comboBox2.DropDownStyle = ComboBoxStyle.DropDown;
            }
            else
            {
                comboBox2.DropDownStyle = ComboBoxStyle.DropDownList;
            }
            if (comboBox2.Text != "自定义")
            {
                try
                {
                    serialPort1.BaudRate = Convert.ToInt32(comboBox2.Text);//设置波特率 
                }
                catch
                {
                    try
                    {
                        serialPort1.Close();//关闭串口
                        button2.Text = "打开串口";
                    }
                    catch { }
                }
            }
            else
            {
                try
                {
                    button2.Text = "打开串口";
                    serialPort1.Close();//关闭串口
                }
                catch { }
            }

        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)//串口号改变
        {
            //if (button2.Text == "关闭串口")
            //{
                //try
                //{
                //    serialPort1.Close();//关闭串口
                //}
                //catch { }
                //serialPort1.PortName = comboBox1.Text;//设置串口号
                //serialPort1.BaudRate = Convert.ToInt32(comboBox2.Text);//设置波特率
                //try
                //{
                //    serialPort1.Open();//打开串口
                //    button2.Text = "关闭串口";
                //}
                //catch
                //{
                //    button2.Text = "打开串口";
                //    MessageBox.Show("串口打开失败！", "错误");
                //} 
            //}
           
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)//窗口关闭处理函数
        {
            try //安全关闭串口，防止占用
            {
                if (comboBox1.Text != "")
                    Settings1.Default.a = comboBox1.Text;
                Settings1.Default.b = comboBox2.Text;
                Settings1.Default.Save();
                string str = serialPort1.ReadExisting();//字符串方式读
                serialPort1.Close(); 
            }   
            catch { }
        }    

        private void button7_Click(object sender, EventArgs e)//保存数据
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "文本文件|*.txt|所有文件|*.*";
            saveFileDialog.FilterIndex = 1;
            saveFileDialog.RestoreDirectory = true;
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                string fName = saveFileDialog.FileName;
                File fSaveAs = new File(fName);
                fSaveAs.WriteFile(textBox1.Text);
            }
        }

        private void label4_Click(object sender, EventArgs e)//1
        {
            if (open == true && NUM >= 0)
            {
                if (isopen[0] == true)
                    isopen[0] = false;
                else
                    isopen[0] = true;
                pictureBox1.Invalidate();//重画pictureBox1
            }
        }

        private void label5_Click_1(object sender, EventArgs e)//2
        {
            if (open == true && NUM >= 1)
            {
                if (isopen[1] == true)
                    isopen[1] = false;
                else
                    isopen[1] = true;
                pictureBox1.Invalidate();//重画pictureBox1
            }
        }

        private void label5_Click(object sender, EventArgs e)//3
        {
            if (open == true && NUM >= 2)
            {
                if (isopen[2] == true)
                    isopen[2] = false;
                else
                    isopen[2] = true;
                pictureBox1.Invalidate();//重画pictureBox1
            }
        }

        private void label7_Click(object sender, EventArgs e)//4
        {
            if (open == true && NUM >= 3)
            {
                if (isopen[3] == true)
                    isopen[3] = false;
                else
                    isopen[3] = true;
                pictureBox1.Invalidate();//重画pictureBox1
            }
        }

        private void label8_Click(object sender, EventArgs e)//5
        {
            if (open == true && NUM >= 4)
            {
                if (isopen[4] == true)
                    isopen[4] = false;
                else
                    isopen[4] = true;
                pictureBox1.Invalidate();//重画pictureBox1
            }
        }

        private void label9_Click(object sender, EventArgs e)//6
        {
            if (open == true && NUM >= 5)
            {
                if (isopen[5] == true)
                    isopen[5] = false;
                else
                    isopen[5] = true;
                pictureBox1.Invalidate();//重画pictureBox1
            }
        }

        private void label10_Click(object sender, EventArgs e)//7
        {
            if (open == true && NUM >= 6)
            {
                if (isopen[6] == true)
                    isopen[6] = false;
                else
                    isopen[6] = true;
                pictureBox1.Invalidate();//重画pictureBox1
            }
        }

        private void label11_Click(object sender, EventArgs e)//8
        {
            if (open == true && NUM >= 7)
            {
                if (isopen[7] == true)
                    isopen[7] = false;
                else
                    isopen[7] = true;
                pictureBox1.Invalidate();//重画pictureBox1
            }
        }

    }
}
public class File   //网上大神封装的file类
{
    string fileName;
    public File(string fileName)
    {
        this.fileName = fileName;
    }
    public string ReadFile()
    {
        try
        {
            StreamReader sr = new StreamReader(fileName, Encoding.Default);
            string result = sr.ReadToEnd();
            sr.Close();
            return result;
        }
        catch (Exception e) { MessageBox.Show(e.Message); }
        return null;
    }
    public void WriteFile(string str)
    {
        try
        {
            StreamWriter sw = new StreamWriter(fileName, false, Encoding.Default);
            sw.Write(str);
            sw.Close();
        }
        catch (Exception e) { MessageBox.Show(e.Message, "保存文件出错！"); }
    }
}