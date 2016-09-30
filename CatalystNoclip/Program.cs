using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;

namespace CatalystNoclip
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // If multiple instances use the AutoHotKey DLL, it will
            // throw an error.
            if (Process.GetProcessesByName("CatalystNoclip").Length > 1)
            {
                MessageBox.Show("You can only run one instance of this app at a time!", "ERROR: Cannot start", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
