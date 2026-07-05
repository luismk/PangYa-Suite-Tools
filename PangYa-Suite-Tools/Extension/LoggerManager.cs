using System;
using System.Collections.Generic;
using System.Text;
using PangYa_Suite_Tools.Logging;

namespace PangYa_Suite_Tools.Extension
{
    public static class LoggerManager
    {
        private static readonly string LogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "activity_log.txt");

        public static void LogAction(string pakName, string actionType, List<string> files, string author = "")
        {
            var details = new StringBuilder();
            details.Append($"{actionType.ToUpperInvariant()} - PAK: {Path.GetFileName(pakName)}");
            if (!string.IsNullOrEmpty(author)) details.Append($" | Author: {author}");
            details.Append($" | Files affected ({files.Count}): {string.Join(", ", files)}");
            AppLogger.Instance.Log("PAK", details.ToString());

            try
            {
                using (StreamWriter sw = File.AppendText(LogPath))
                {
                    sw.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{actionType.ToUpper()}] - PAK: {Path.GetFileName(pakName)}");

                    if (!string.IsNullOrEmpty(author))
                    {
                        sw.WriteLine($"   👤 Author: {author}");
                    }

                    sw.WriteLine($"   📦 Files affected ({files.Count}):");
                    foreach (var file in files)
                    {
                        sw.WriteLine($"      -> {file}");
                    }
                    sw.WriteLine(new string('-', 60));
                }
            }
            catch
            {
                // Previne que falhas de escrita de log travem o aplicativo principal
            }
        }

        public static void OpenLogFile()
        {
            if (File.Exists(LogPath))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = LogPath,
                    UseShellExecute = true // Abre com o editor padrão do sistema (Ex: Bloco de Notas)
                });
            }
        }
    }
}
