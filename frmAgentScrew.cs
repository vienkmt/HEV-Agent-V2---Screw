using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Telerik.WinControls;
using Telerik.WinControls.UI;
using log4net;
using log4net.Config;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using System.Net;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Win32;

namespace HEV_Agent_V2
{
    public partial class frmAgentScrew : Telerik.WinControls.UI.RadForm
    {
       
        DataSet DtSet;
        DataTable TbMachine;
        private static readonly ILog log = LogManager.GetLogger(typeof(frmAgentScrew));
        MqttClient client = null;
        string clientId = "";
        bool active = true;
        RegistryKey HevAgent = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
        string LogsPath = "";
        string ServerIp = "";
        string Note = "";

        public frmAgentScrew()
        {

            LogsPath = Properties.Settings.Default.LogPath;
            ServerIp = Properties.Settings.Default.Server;
            Note = Properties.Settings.Default.Note;

           

            DtSet = new System.Data.DataSet();
            TbMachine = DtSet.Tables.Add("Lineeee");
            InitializeComponent();
            BasicConfigurator.Configure();
            client = new MqttClient(IPAddress.Parse(ServerIp));
            client.MqttMsgPublishReceived += client_MqttMsgPublishReceived;
            client.MqttMsgPublished += client_MqttMsgPublished;
            client.MqttMsgSubscribed += client_MqttMsgSubscribed;
            client.MqttMsgUnsubscribed += client_MqttMsgUnsubscribed;
            client.ConnectionClosed += client_Dis;
            //  client.ConnectionClosed += ConnectionClosedEventHandler;
            //client.Connect
            clientId = Guid.NewGuid().ToString();

            TbMachine = DtSet.Tables.Add("Machine");
                 
            TbMachine.Columns.Add("Stt", typeof(System.String));
            TbMachine.Columns.Add("LastTime", typeof(System.String));
            TbMachine.Columns.Add("Name", typeof(System.String));
            TbMachine.Columns.Add("Status", typeof(System.String));
            TbMachine.Columns.Add("ErrorCode", typeof(System.String));

            radGridView1.DataSource = TbMachine;
            this.radGridView1.MasterTemplate.Columns[0].Width = 50;
            this.radGridView1.MasterTemplate.Columns[1].Width = 150;
            this.radGridView1.MasterTemplate.Columns[2].Width = 120;
            this.radGridView1.MasterTemplate.Columns[3].Width = 100;
            this.radGridView1.MasterTemplate.Columns[4].Width = 120;
        
            this.radGridView1.TableElement.RowHeight = 30;
            this.radGridView1.TableElement.TableHeaderHeight = 40;


            //Khởi động cùng windows
            HevAgent.DeleteValue("HEV_Agent", false);
            HevAgent.SetValue("HEV_Agent", Application.ExecutablePath);

            //Lấy setting
            //Ẩn vẫn chạy
            //Build json gửi lên server
            //Nếu server gọi thì trả lời








        }


        //PhanTich
        private void PhanTich(string FileName)
        {

            try
            {
                //Tách ra các thông tin cần thiết trước
                string[] ThongTin = FileName.Split(',');
                string Ten = ThongTin[5].Trim();
                string Code = ThongTin[2].Trim();
                bool co = false;
                

                foreach (DataRow row in TbMachine.Rows)
                {
                    //Nếu có rồi thì update vào hàng đó
                    if (row[2].ToString()==Ten)
                    {
                        row["LastTime"]= DateTime.Now.ToString();
                        row["ErrorCode"] = Code;
                        
                        
                        if (Code == "9001" || Code == "9002" || Code == "9003" || Code == "9004")
                            row["Status"] = "1";
                        else
                            row["Status"] = "0";


                        Console.WriteLine("Đã có");
                        co = true;
                        return;
                    }

                }

                //Nếu chưa có hàng đó thì tạo hàng
                if (!co)
                {
                    var hang = TbMachine.NewRow();
                    hang["LastTime"] = DateTime.Now.ToString();
                    hang["Name"] = Ten;
                    hang["ErrorCode"] = Code;
                    if(Code=="9001" || Code == "9002" || Code == "9003" || Code == "9004")
                        hang["Status"] = "1";
                    else
                        hang["Status"] = "0";
                    TbMachine.Rows.Add(hang);
                }


            }

            catch (Exception ex) 
            {
                log.Error(ex);
            }
            finally
            {
                this.radGridView1.CurrentRow = null;
            }




        }



        private void fileSystemWatcher1_Created(object sender, System.IO.FileSystemEventArgs e)
        {
            Debug.WriteLine(e.Name);
            PhanTich(e.Name);
        }
        
        
        
        
        Font pop = new Font("Consolas", 12f, FontStyle.Bold);
        private void radGridView1_ViewCellFormatting(object sender, CellFormattingEventArgs e)
        {
            GridHeaderCellElement cell = e.CellElement as GridHeaderCellElement;
            if (cell != null)
            {
                cell.Font = pop;
                cell.ForeColor = Color.Black;

            }

            if (e.CellElement.Text == "0")
                e.CellElement.Text = "NG";
            if (e.CellElement.Text == "1")
                e.CellElement.Text = "OK";
            if (e.CellElement.Text == "2")
                e.CellElement.Text = "OFF";
            if (e.CellElement.Text == "3")
                e.CellElement.Text = "WAIT";
        }

        private void frmAgentScrew_Load(object sender, EventArgs e)
        {
            //Khi khởi động lên, cố gắng kết nối tới server
            try
            {
                fileSystemWatcher1.Path = LogsPath;
            }

            catch (Exception ex)
            {
                log.Error(ex);
            }


            backgroundWorker1.RunWorkerAsync();
        }

        //Nhận tin nhắn
        private void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            //Nhận
            string result = System.Text.Encoding.UTF8.GetString(e.Message);
            Console.WriteLine(result);
            //  richTextBox1.Text = DateTime.Now.ToString() + "    " + result;

            //richTextBox1.Invoke(new MethodInvoker(() => { richTextBox1.Text = DateTime.Now.ToString() + "  ===  " + result; }));


        }

        private void client_MqttMsgUnsubscribed(object sender, MqttMsgUnsubscribedEventArgs e)
        {
            // write your code
        }

       //Sub rồi
        private void client_MqttMsgSubscribed(object sender, MqttMsgSubscribedEventArgs e)
        {
            // write your code
            Console.WriteLine("Subscribed");
        }

        //Published
        void client_MqttMsgPublished(object sender, MqttMsgPublishedEventArgs e)
        {
            Console.WriteLine("Published");

        }
      
        
        
        //Nếu ngắt kết nối thì sao?
        void client_Dis(object sender, EventArgs e)
        {
            Debug.WriteLine("Tự dưng bị ngắt kết nối");
            //Co gang connect lại
          //  Task.Run(() => PersistConnectionAsync());
        }


        void ConnectionClosedEventHandler(object sender, EventArgs e)
        {

            Debug.WriteLine("Kết nối vừa bị đóng vì cái gì đó");
        }



        private async Task PersistConnectionAsync()
        {
            var connected = client.IsConnected;
            while (active)
            {
                if (!connected)
                {
                    try
                    {
                        client.Connect(clientId);
                        Debug.WriteLine("ReConnected");
                        string[] topic = { "vienkmtt" };
                        client.Subscribe(topic, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
                    }
                    catch
                    {
                        Debug.WriteLine("failed reconnect");
                    }
                }

                if (client.IsConnected)
                {
                    // Debug.WriteLine("ReConnected");
                    //  string[] topic = { "vienkmtt" };
                    //   client.Subscribe(topic, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });

                }

                await Task.Delay(1000);
                connected = client.IsConnected;
            }
        }

        private void frmAgentScrew_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (client.IsConnected)
                client.Disconnect();

            active = false;
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            while (true)
            {
                if (!client.IsConnected)
                {
                    try
                    {
                        client.Connect(clientId,
                        null, null,
                        false,
                        MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE, //aws 
                        true,
                        $"hev_ngatketnoi",
                        "{\"message\":\"e abc vua bi disconnected\"}",
                        true,
                        5);

                        if (client.IsConnected)
                        {
                            Debug.WriteLine("Connected to Server");
                            this.radWaitingBar1.Invoke(new MethodInvoker(() => { this.radWaitingBar1.StartWaiting(); this.radWaitingBar1.Text = "Connected to Server. Monitoring....."; this.radWaitingBar1.ForeColor = Color.Blue; }));

                        }


                        string[] topic = { "hev_thongbao" };
                        client.Subscribe(topic, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
                    }
                    catch
                    {
                        Debug.WriteLine("Error. Can't connect to Server");
                        this.radWaitingBar1.Invoke(new MethodInvoker(() => { this.radWaitingBar1.StopWaiting(); this.radWaitingBar1.Text = "Have an error. Can't connect to Server."; this.radWaitingBar1.ForeColor = Color.Red; }));
                    }
                }

                Thread.Sleep(500);
            }



        }

        private void frmAgentScrew_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            notifyIcon1.Visible = true;
            this.Hide();


        }
    }
}
