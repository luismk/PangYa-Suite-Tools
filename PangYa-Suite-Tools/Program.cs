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

            // 1. Verifica se a aplicação ATUAL já possui privilégios de administrador
            if (!IsRunningAsAdmin())
            {
                try
                {
                    // Configura o processo para reiniciar solicitando elevação (runas)
                    ProcessStartInfo procInfo = new ProcessStartInfo
                    {
                        FileName = Environment.ProcessPath, // Pega o caminho do próprio .exe de forma segura (.NET 6+)
                        UseShellExecute = true,
                        Verb = "runas" // Comando nativo do Windows para pedir permissão de Admin
                    };

                    // REPASSA OS ARGUMENTOS: Se o usuário abriu clicando num arquivo .pak, 
                    // precisamos passar esse caminho para a nova instância elevada!
                    if (args != null && args.Length > 0)
                    {
                        procInfo.Arguments = $"\"{string.Join("\" \"", args)}\"";
                    }

                    // Inicia o processo com privilégios e fecha a instância atual que é "comum"
                    Process.Start(procInfo);
                    return;
                }
                catch (Exception)
                {
                    // Caso o usuário clique em "Não" na tela de aviso do Windows (UAC)
                    MessageBox.Show(
                        Strings.Program_ThisApplicationRequiresAdministrativePrivilegesTo,
                        Strings.Program_AdminPrivilegesRequired, MessageBoxButtons.OK, MessageBoxIcon.Stop);
                    return;
                }
            }

            // 2. SE CHEGOU AQUI, O APP JÁ É ADMINISTRADOR: Executa a lógica padrão
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

        // Função auxiliar para checar o privilégio
        private static bool IsRunningAsAdmin()
        {
            try
            {
                WindowsIdentity identity = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }
    }
}
