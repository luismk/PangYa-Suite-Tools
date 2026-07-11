using System;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using System.Windows.Forms;
using PangYa_Suite_Tools.Localization;

namespace PangYa_Suite_Tools
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            ApplicationConfiguration.Initialize();
            LocalizationManager.Initialize();

            if (args != null && args.Length > 0)
            {
                string filePath = args[0];

                if (File.Exists(filePath) && filePath.EndsWith(".pak", StringComparison.OrdinalIgnoreCase))
                {
                    Application.Run(new FrmPakMaker("en",filePath));
                    return;
                }
            }

            Application.Run(new FrmMenu());
        }
    }
}
