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
using Newtonsoft.Json;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Web.UI.WebControls;
using System.Data.SqlClient;
using System.Data.Common;

namespace HEV_Agent_V2
{
    public partial class frmMqttServer : Telerik.WinControls.UI.RadForm
    {
        SqlConnection conn = DBUtils.GetDBConnection();
        SqlConnection conn2 = DBUtils.GetDBConnection();
        string sql = "";
        DataSet DtSet;
        DataTable TbMachine;
        private static readonly ILog log = LogManager.GetLogger(typeof(frmMqttServer));
        MqttClient client = null;
        string clientId = "";
        bool active = true;
        bool exit = false;
        RegistryKey HevAgent = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
        string LogsPath = "";
        string ServerIp = "";
        string Note = "";string Ip = "1.1.1.2";

        public frmMqttServer()
        {

            LogsPath = Properties.Settings.Default.LogPath;
            ServerIp = Properties.Settings.Default.Server;
            Note = Properties.Settings.Default.Note;



             DtSet = new System.Data.DataSet();
            TbMachine = DtSet.Tables.Add("Lineeee");
            InitializeComponent();
            BasicConfigurator.Configure();
            client = new MqttClient("127.0.0.1");
            client.MqttMsgPublishReceived += client_MqttMsgPublishReceived;
            client.MqttMsgPublished += client_MqttMsgPublished;
            client.MqttMsgSubscribed += client_MqttMsgSubscribed;
            client.ConnectionClosed += client_Dis;
            //  client.ConnectionClosed += ConnectionClosedEventHandler;
            //client.Connect
            clientId = Guid.NewGuid().ToString();

            TbMachine = DtSet.Tables.Add("Machine");
                 
            TbMachine.Columns.Add("LastTime", typeof(System.String));
            TbMachine.Columns.Add("Name-IP", typeof(System.String));
            TbMachine.Columns.Add("Status", typeof(System.Int32));
            TbMachine.Columns.Add("ErrorCode", typeof(System.String));
            TbMachine.Columns.Add("Change", typeof(System.String));
            TbMachine.Columns.Add("Note", typeof(System.String));
            TbMachine.Columns.Add("InDB", typeof(System.String));


            radGridView1.DataSource = TbMachine;
            this.radGridView1.MasterTemplate.Columns[0].Width = 150;
            this.radGridView1.MasterTemplate.Columns[1].Width = 150;
            this.radGridView1.MasterTemplate.Columns[2].Width = 100;
            this.radGridView1.MasterTemplate.Columns[3].Width = 120;
            this.radGridView1.MasterTemplate.Columns[4].Width = 120;
            this.radGridView1.MasterTemplate.Columns[5].Width = 120;
            this.radGridView1.MasterTemplate.Columns[6].Width = 120;


            this.radGridView1.TableElement.RowHeight = 25;
            this.radGridView1.TableElement.TableHeaderHeight = 40;

            try
            {
                Ip = GetLocalIPAddress();
            }
            catch { }

            //Khởi động cùng windows
            HevAgent.DeleteValue("HEV_Mqtt_Server", false);
            HevAgent.SetValue("HEV_Mqtt_Server", Application.ExecutablePath);

            txtLogPath.Text = LogsPath;
            txtServer.Text = ServerIp;
            txtNote.Text = Note;

        }
        



        //Nhận tin nhắn, Xử lý dữ liệu nhận được ở đây
        private void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            //Nếu nhiều quá xoá đi, giữ cỡ 500 bản ghi thôi
            if (TbMachine.Rows.Count > 500)
                TbMachine.Clear();

            try
            {
                string result = System.Text.Encoding.UTF8.GetString(e.Message);

                if (result != "")
                {

                    //Xử lý dữ liệu nhận được ở đây
                    var eqm = Eqm.FromJson(result);
                    foreach(var machine in eqm)
                    {

                        //Tách lấy ip và name để định danh
                        //Tách lấy dữ liệu
                        string diachi2 = machine.Name + "-" + machine.Ip;
                        diachi2 = diachi2.ToLower().Trim();
                        int trangthai = machine.Status;
                        string ErrorCode = machine.ErrorCode.Trim();
                       
                        this.radGridView1.Invoke(new MethodInvoker(() => {
                            this.radGridView1.BeginUpdate();

                        }));


                        var hang = TbMachine.NewRow();
                        hang["LastTime"] = machine.LastTime;
                        hang["Name-IP"] = diachi2;
                        hang["Status"] = trangthai.ToString();
                        hang["ErrorCode"] = ErrorCode;
                        hang["Note"] = machine.Note;
                        hang["InDB"] = "YES";
                        hang["Change"] = "YES";


                        //Truy xuất vào cơ sở dữ liệu, xem trạng thái đưa lên có # vs trong csdl không?
                        //Nếu khác thì insert log + update where address=idđ;;;;;
                        string sql_get_stt = "SELECT TOP 1 [TrangThai],[PointId],[Ten],[Process] FROM [hev].[dbo].[Points] WHERE DiaChi2='" + diachi2+"'";
                        // Debug.WriteLine(sql_get_stt);

                        try
                        {
                            DbDataReader kq = mysql(sql_get_stt);
   
                            if (kq.HasRows)
                            {
                                int old_stt = 0;
                                int PointPoint = 0;
                                string eqm_name = "";
                                string Process = "";
                               

                                while (kq.Read())
                                {
                                    old_stt = Int16.Parse(kq.GetValue(0).ToString());
                                    PointPoint = Int16.Parse(kq.GetValue(1).ToString());
                                    eqm_name = kq.GetValue(2).ToString();
                                    Process= kq.GetValue(3).ToString();
                                   // Debug.WriteLine(eqm_name);

                                }
                                conn.Close();

                                if (trangthai != old_stt)
                                {
                                    // update đi cưng
                                    //
                                    string SqlInsert = "";
                                    string SqlUpdate = "UPDATE Points SET ThoiGian2=(getdate()), TrangThai = " + trangthai + ",ErrorCode='"+ ErrorCode+"' WHERE [PointId]=" + PointPoint;

                                    if (trangthai == 1)
                                        SqlInsert = "; INSERT INTO [dbo].[Logs] ([TrangThai] ,[PointId]) VALUES (" + trangthai + "," + PointPoint + ")";
                                    else
                                        SqlInsert = "; INSERT INTO [dbo].[Logs] ([TrangThai] ,[PointId],[ErrorCode],[Process]) VALUES (" + trangthai + "," + PointPoint + ",'" + ErrorCode + "','" + Process + "')";


                                    Debug.WriteLine(SqlUpdate);
                                    Debug.WriteLine(SqlInsert);
                                    //inseert


                                    conn2.Open();
                                    SqlCommand cmd2 = conn2.CreateCommand();
                                    cmd2.CommandText = SqlUpdate + SqlInsert;
                                    cmd2.ExecuteNonQuery();
                                   // listBox2.Invoke(new MethodInvoker(() => { listBox2.Items.Add(DateTime.Now.ToString() + " === " + eqm_name + " - ID: " + diachi2 + " ==== Changed from " + old_stt + " to " + trangthai); }));
                                    conn2.Close();
                                }
                                else
                                    //listBox2.Invoke(new MethodInvoker(() => { listBox2.Items.Add(DateTime.Now.ToString() + " === " + eqm_name + " - ID: " + diachi2 + " ==== No change"); }));
                                    hang["Change"] = "NO";

                            }
                            else
                            {
                              // listBox2.Invoke(new MethodInvoker(() => { listBox2.Items.Add(DateTime.Now.ToString() + " === EQM at: " + diachi2 + ". == Note: " + machine.Note+" == 404"); }));
                                hang["InDB"] = "NO NO NO NO NO";
                                hang["Change"] = "===";
                                conn.Close(); 
                            }
                            TbMachine.Rows.Add(hang);

                        }

                        catch (Exception ex)
                        {
                            Debug.WriteLine("C254. Lỗi khi cập nhật trạng thái point: " + ex.ToString());
                            log.Error("C180. Loi khi cap nhat vao csdl");

                        }

                        this.radGridView1.Invoke(new MethodInvoker(() => {
                            this.radGridView1.EndUpdate();
                            this.radGridView1.TableElement.ScrollToRow(radGridView1.Rows.Last());

                        }));




                    }

                }

            }
            catch (Exception ex)
            {
                log.Error("C252. Loi khi nhan du lieu: "+ex);
            }
        }


        //Non q
        private int SqlNonQuery(string sqlcmd)
        {
            conn2.Open();
            SqlCommand cmd = new SqlCommand(sqlcmd, conn);
            int kq = cmd.ExecuteNonQuery();
            conn2.Close();
            return kq;
        }
        private DbDataReader mysql(string sqlcmd)
        {
            conn.Open();
            SqlCommand cmd = new SqlCommand(sqlcmd, conn);
            DbDataReader ketqua = cmd.ExecuteReader();
            return ketqua;

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
                string d = DateTime.Now.ToString();
                int stt = 0;
                bool co = false;
                
                if (Code == "9001" || Code == "9002" || Code == "9003" || Code == "9004")
                    stt = 1;



                foreach (DataRow row in TbMachine.Rows)
                {
                    //Nếu có rồi thì update vào hàng đó
                    if (row["Name"].ToString()==Ten)
                    {
                        row["LastTime"] = d;
                        row["ErrorCode"] = Code;
                        row["Status"] = stt;
                        Console.WriteLine("Đã có trong csdl, update thôi");
                        co = true;
                        
                    }

                }

                //Nếu chưa có hàng đó thì tạo hàng
                if (!co)
                {
                    var hang = TbMachine.NewRow();
                    hang["LastTime"] = d;
                    hang["Name"] = Ten;
                    hang["Status"] = stt;
                    hang["ErrorCode"] = Code;
                    hang["Note"] = Note;
                    hang["Ip"] = Ip;

                    TbMachine.Rows.Add(hang);
                }

                //Dù sao đi chăng nữa thì cũng Pub lên server thôi mà Man
                //Build Json rồi Pub lên kênh
                if (client.IsConnected)
                {
                    //client.Publish("hev",);

                    //[{"LastTime":"17-Nov-20 5:32:10 PM","Name":"COM101","Status":0,"ErrorCode":"SV6H3110"}]

                    string abc= "[{\"LastTime\":\""+d+"\",\"Name\":\""+Ten+"\",\"Status\":"+stt+",\"ErrorCode\":\""+Code+ "\",\"Note\":\"" + Note + "\",\"Ip\":\"" + Ip + "\"}]";
                    //string data = TbToJson(TbMachine);
                    client.Publish("hev", Encoding.UTF8.GetBytes(abc), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);
                    Debug.WriteLine("Vừa pub dữ liệu lẻ lên server đó a");

                }
                else
                {
                    //Debug.WriteLine("K kết nối tới server");
                    log.Error("C160. Khong Publish du lieu len Server duoc");
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

        public static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new Exception("No network adapters with an IPv4 address in the system!");
        }

        public string TbToJson(DataTable table)
        {
            string JSONString = string.Empty;
            JSONString = JsonConvert.SerializeObject(table);
            return JSONString;
        }

        private void fileSystemWatcher1_Created(object sender, System.IO.FileSystemEventArgs e)
        {
            Debug.WriteLine(e.Name);
            PhanTich(e.Name);
        }
        
       
        Font pop = new Font("Consolas", 11f, FontStyle.Bold);
        private void radGridView1_ViewCellFormatting(object sender, CellFormattingEventArgs e)
        {
            GridHeaderCellElement cell = e.CellElement as GridHeaderCellElement;
            if (cell != null)
            {
                cell.Font = pop;
                cell.ForeColor = Color.Black;
                cell.BackColor = Color.AliceBlue;

            }

            if (e.CellElement.ColumnIndex == 2) { 
                if (e.CellElement.Text == "0") { 
                    e.CellElement.Text = ""; 
                    e.CellElement.BackColor = Color.Red;
                    e.CellElement.TextAlignment = ContentAlignment.MiddleCenter;

                }

                if (e.CellElement.Text == "1") { 
                    e.CellElement.Text = "";
                    e.CellElement.BackColor = Color.LimeGreen;
                    e.CellElement.TextAlignment= ContentAlignment.MiddleCenter;
                }

                if (e.CellElement.Text == "2") {
                    e.CellElement.Text = "";  e.CellElement.BackColor = Color.Gray;
                    e.CellElement.TextAlignment = ContentAlignment.MiddleCenter;
                }

                if (e.CellElement.Text == "3")
                    e.CellElement.Text = "";
            }

            

            }

        private void frmAgentScrew_Load(object sender, EventArgs e)
        {
            //Khi khởi động lên, cố gắng kết nối tới server
            try
            {
                backgroundWorker1.RunWorkerAsync();
            }

            catch (Exception ex)
            {
                log.Error(ex);
            }

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
            Console.WriteLine("Published: "+e.MessageId);

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
                        MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, //aws 
                        true,
                        $"hev_ngatketnoi",
                        "{\"message\":\"e abc vua bi disconnected\"}",
                        true,
                        5);

                        if (client.IsConnected)
                        {
                            Debug.WriteLine("Connected to Server");
                            this.radWaitingBar1.Invoke(new MethodInvoker(() => { this.radWaitingBar1.StartWaiting(); this.radWaitingBar1.Text = "Connected. Tracking now....."; this.radWaitingBar1.ForeColor = Color.Blue; }));

                        }

                        //Sub vào kênh này để xem thông tin
                        string[] topic = { "hev" };
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
            if (exit) { 
                client.Disconnect();

            }

            else {
                e.Cancel = true;
                notifyIcon1.Visible = true;
                this.Hide();
            }


        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            //FrmAgent.ActiveForm();
            if (this.WindowState == FormWindowState.Normal)
            {
                Show();
                this.Focus();

            }
            else if (this.WindowState == FormWindowState.Minimized)
            {
                Show();
                this.WindowState = FormWindowState.Normal;
                this.Focus();
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {

            if (txtPass.Text == "hev123")
            {
                Properties.Settings.Default.LogPath = txtLogPath.Text;
                Properties.Settings.Default.Note = txtNote.Text;
                Properties.Settings.Default.Server = txtServer.Text.Trim();
                Properties.Settings.Default.Save();
                MessageBox.Show("Save OK");
                exit = true;
                Application.Restart();

            }
                
            else
                MessageBox.Show("Sai Pass");
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            if (txtPass.Text == "hev123")
            {
                
                exit = true;
                Application.Exit();

            }

            else
                MessageBox.Show("Sai Pass");
        }

        private void radPageView1_SelectedPageChanged(object sender, EventArgs e)
        {

        }

        private void backgroundWorker2_DoWork(object sender, DoWorkEventArgs e)
        {

        }
    }
}
