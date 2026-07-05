using System;
using System.Threading;
using System.Windows.Forms;
using SimpleAutoDuck.UI;

namespace SimpleAutoDuck
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            bool createdNew;
            using (var singleInstance = new Mutex(true, @"Local\SimpleAutoDuck.SingleInstance", out createdNew))
            {
                if (!createdNew)
                    return;

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm());
            }
        }
    }
}
