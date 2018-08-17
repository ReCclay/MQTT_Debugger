using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace MQTT
{
    public partial class Form1 : Form
    {
        //窗口自适应
        float xvalues;
        float yvalues;

        string UserName = "CLAY";
        string PassWord = "11223344";
        string IPAdress = "140.143.4.×";
        int Port = 1883;
        string Subscribed = "/sub";//订阅的主题
        string Published = "/pub";//发布的主题
        string clientid = "";
        MqttClient client = null;
        MqttClient clientCopy = null;
        Boolean IsConnectFlage = false;
        private delegate void ShowData(string str);//定义一个委托
        private delegate void NowStatus(Boolean boole, string str1, string str2);//定义一个委托
        private ShowData showData;
        private NowStatus nowStatus;
        private Thread ThreadConnectService;//连接服务器线程
        public Form1()
        {
            InitializeComponent();

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            showData = new ShowData(ShowDataMethod);//实例化
            nowStatus = new NowStatus(NowStatusMethod);//实例化
            //初始化
            textBox1.Text = UserName;
            textBox2.Text = PassWord;
            textBox3.Text = IPAdress;
            textBox4.Text = Port.ToString();
            textBox5.Text = Subscribed;
            textBox6.Text = Published;
            //客户端ID获取，实现多开！
            clientid = GetNetworkAdpaterID() + GetTimeStamp();

            //窗口自适应
            this.Resize += new EventHandler(MainForm_Resize); //添加窗体拉伸重绘事件
            xvalues = this.Width;//记录窗体初始大小
            yvalues = this.Height;
            SetTag(this);

        }
        //窗口自适应
        private void MainForm_Resize(object sender, EventArgs e)//重绘事件
        {
            float newX = this.Width / xvalues;//获得比例
            float newY = this.Height / yvalues;
            SetControls(newX, newY, this);
        }
        //窗口自适应
        private void SetControls(float newX, float newY, Control cons)//改变控件的大小
        {
            foreach (Control con in cons.Controls)
            {
                string[] mytag = con.Tag.ToString().Split(new char[] { ':' });
                float a = Convert.ToSingle(mytag[0]) * newX;
                con.Width = (int)a;
                a = Convert.ToSingle(mytag[1]) * newY;
                con.Height = (int)a;
                a = Convert.ToSingle(mytag[2]) * newX;
                con.Left = (int)a;
                a = Convert.ToSingle(mytag[3]) * newY;
                con.Top = (int)a;
                Single currentSize = Convert.ToSingle(mytag[4]) * newY;

                con.Font = new Font(con.Font.Name, currentSize, con.Font.Style, con.Font.Unit);
                if (con.Controls.Count > 0)
                {
                    SetControls(newX, newY, con);
                }
            }
        }
        //窗口自适应
        /// <summary>
        /// 遍历窗体中控件函数
        /// </summary>
        /// <param name="cons"></param>
        private void SetTag(Control cons)
        {
            foreach (Control con in cons.Controls)  //遍历窗体中的控件,记录控件初始大小
            {
                con.Tag = con.Width + ":" + con.Height + ":" + con.Left + ":" + con.Top + ":" + con.Font.Size;
                if (con.Controls.Count > 0)
                {
                    SetTag(con);
                }
            }
        }

        /*显示接收的消息*/
        private void ShowDataMethod(string str)
        {
            textBox8.AppendText(str);
        }
        /*状态显示*/
        private void NowStatusMethod(Boolean boole, string str1, string str2)
        {
            label8.Text = str1;
            IsConnectFlage = boole;
            button1.Text = str2;
        }
        //连接按钮
        private void button1_Click(object sender, EventArgs e)
        {
            if (IsConnectFlage == false)
            {
                try
                {
                    try { ThreadConnectService.Abort(); }//先清除一下以前的
                    catch (Exception) { }
                    ThreadConnectService = new Thread(ConncetService);//把连接服务器的函数加入任务
                    ThreadConnectService.Start();//启动任务
                }
                catch (Exception)
                {
                    button1.Invoke(nowStatus, false, "连接断开", "连接");
                }
            }
            else
            {
                try { client.Disconnect(); }
                catch (Exception) { }
                try { clientCopy.Disconnect(); }
                catch (Exception) { }
                button1.Invoke(nowStatus, false, "连接断开", "连接");
            }
        }

        //发送窗口的清空按钮 
        private void button3_Click(object sender, EventArgs e)
        {
            textBox7.Text = null;
        }
        //接收窗口的清空 按钮
        private void button4_Click(object sender, EventArgs e)
        {
            textBox8.Text = null;
        }

        /*连接服务器函数*/
        private void ConncetService()
        {
            try
            {
                UserName = textBox1.Text;
                PassWord = textBox2.Text;
                IPAdress = textBox3.Text;
                Port = int.Parse(textBox4.Text);
                Subscribed = textBox5.Text;
                Published = textBox6.Text;

                //MQTT通信必备!
                string[] SubscribedTopic = new string[] { Subscribed };//订阅的主题
                byte[] qosLevels = new byte[] { MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE }; // qos=0

                client = new MqttClient(IPAdress, Port,
                                                     false, // 不开启TLS
                                                     MqttSslProtocols.TLSv1_0, // TLS版本
                                                     null,
                                                     null
                                                    );
                byte code = client.Connect(clientid,
                                            UserName,
                                            PassWord,
                                            true, // cleanSession
                                            60); // keepAlivePeriod
                clientCopy = client;
                if (client.IsConnected)
                {
                    client.MqttMsgPublishReceived += client_MqttMsgPublishReceived;
                    client.MqttMsgSubscribed += client_MqttMsgSubscribed;
                    client.MqttMsgUnsubscribed += client_MqttMsgUnsubscribed;
                    client.MqttMsgPublished += client_MqttMsgPublished;
                    client.ConnectionClosed += client_ConnectionClosed;
                    client.Subscribe(SubscribedTopic, qosLevels); // sub 的qos=1

                    button1.Invoke(nowStatus, true, "连接成功", "断开");
                }
            }
            catch (Exception)
            {
                try
                {
                    button1.Invoke(nowStatus, false, "连接失败", "连接");
                }
                catch (Exception)
                {
                }

            }
        }

        //下面这几个可以在未成功的时候进行调试用，尤其首选MessageBox.show(“×××”,“提示”);
        
        // sub后的操作
        private void client_MqttMsgSubscribed(object sender, MqttMsgSubscribedEventArgs e)
        {
            /*成功订阅了主题*/
        }
        // 接受消息后的操作
        private void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            textBox8.Invoke(showData, Encoding.UTF8.GetString(e.Message));
        }
        // 发布消息后的操作
        private void client_MqttMsgPublished(object sender, MqttMsgPublishedEventArgs e)
        {

        }
        // 关闭连接后的操作
        private void client_ConnectionClosed(object sender, EventArgs e)
        {
            try
            {
                button1.Invoke(nowStatus, false, "连接断开", "连接");
            }
            catch (Exception)
            {
            }

        }
        // 取消sub后的操作
        private void client_MqttMsgUnsubscribed(object sender, MqttMsgUnsubscribedEventArgs e)
        {

        }

        /// <summary>
        /// 获取MAC地址
        /// </summary>
        /// <returns></returns>
        public static string GetNetworkAdpaterID()
        {
            try
            {
                string mac = "";

                ManagementClass mc = new ManagementClass("Win32_NetworkAdapterConfiguration");
                ManagementObjectCollection moc = mc.GetInstances();
                foreach (ManagementObject mo in moc)
                    if ((bool)mo["IPEnabled"] == true)
                    {
                        mac += mo["MacAddress"].ToString() + " ";
                        break;
                    }
                moc = null;
                mc = null;
                return mac.Trim();
            }
            catch (Exception e)
            {
                return "uMnIk";
            }
        }

        /// <summary>  
        /// 获取时间戳  
        /// </summary>  
        /// <returns></returns>  
        public static string GetTimeStamp()
        {
            TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return Convert.ToInt64(ts.TotalSeconds).ToString();
        }

        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                client.Publish(textBox6.Text, Encoding.UTF8.GetBytes(textBox7.Text.ToString()),
                0, false);//`Encoding.UTF8.GetBytes`将字符串转换为UTF8编码的字节数组
            }
            catch (Exception)
            {
                button1.Invoke(nowStatus, false, "发送失败", "连接");
            }

        }

        //窗口关闭
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            try { client.Disconnect(); }
            catch (Exception) { }
            try { clientCopy.Disconnect(); }
            catch (Exception) { }
        }

    }
}
