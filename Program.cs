using System;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace HEV_Agent_V2
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            bool createdNew;

            Mutex m = new Mutex(true, "HEV_AGENT", out createdNew);
            if (!createdNew)
            {
                // myApp is already running...
                MessageBox.Show("Phần mềm đang chạy, tắt trước đi đã");
                return;
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new frmMqttServer());
        }
    }
}