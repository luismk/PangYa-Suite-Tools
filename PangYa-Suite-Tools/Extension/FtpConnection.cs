using FluentFTP;
using PangyaAPI.UpdateList.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace PangYa_Suite_Tools.Extension
{
    class FtpConnection
    {
        public string host = "127.0.0.1";
        public string UserId = "pangya";
        public string Password = "123456";
        public string path = "/";
        public string port = "21";
        FtpClient client { get; set; }
        public bool IsConnected;
        public FtpConnection()
        {
            client = new FtpClient();
            IsConnected = false;
        }

        public FtpConnection(string host, string userId, string password, string path, string port)
        {
            this.host = host;
            UserId = userId;
            Password = password;
            this.port = port;
            client = new FtpClient();
            IsConnected = false;
        }

        public bool CheckConnection()
        {
            try
            {
                if (client == null || !client.IsConnected)
                {
                    client = new FtpClient(host, UserId, Password, int.Parse(port));
                    client.Connect();
                    IsConnected = client.IsConnected;
                }
                return client.IsConnected;
            }
            catch { IsConnected = false; return false; }
        }


        public bool UploadAllFiles(string _localZipFolder, ProgressBar rootbar, Label labeldesc)
        {
            int count = 0;
            CheckConnection();
            string[] fileEntries = Directory.GetFiles(_localZipFolder);
            MethodInvoker action = delegate
            { rootbar.Maximum = fileEntries.Length; };
            rootbar.BeginInvoke(action);
            foreach (string fileName in fileEntries)
            {
                if (fileName.Contains(".zip") || fileName.Contains("updatelist"))
                {
                    { labeldesc.Text = fileName; }
                    ;
                    labeldesc.BeginInvoke(action);
                    count++;
                    { rootbar.Value = count; }
                    ;
                    rootbar.BeginInvoke(action);
                    string zipName = fileName.Split('\\').Last();
                    var result = client.UploadFile(fileName, "/" + zipName);
                    if (result== FtpStatus.Success)
                    {
                        Debug.WriteLine("Sucess send File!");
                    }
                }
            }
            client.Disconnect();
            return true;
        }

        public bool UploadAllFiles(Dictionary<string, UpdateEntry> UpdateFileList, ProgressBar rootbar, Label labeldesc)
        {
            int count = 0;
            CheckConnection();
            MethodInvoker action = delegate
            { rootbar.Maximum = UpdateFileList.Count; };
            rootbar.BeginInvoke(action);
            foreach (string i in UpdateFileList.Keys)
            {
                string? fullPath = UpdateFileList[i].FullPath;
                if (string.IsNullOrWhiteSpace(fullPath) || !Directory.Exists(fullPath))
                    continue; // TODO: Should add logging or error handling here to indicate that the directory does not exist.
                string[] fileEntries = Directory.GetFiles(fullPath);
                foreach (var fileName in fileEntries)
                {
                    { labeldesc.Text = UpdateFileList[fileName].pname; }
                    ;
                    labeldesc.BeginInvoke(action);
                    count++;
                    { rootbar.Value = count; }
                    ;
                    rootbar.BeginInvoke(action);
                    string zipName = fileName.Split('\\').Last();
                    var result = client.UploadFile(fileName, "/" + zipName);
                    if (result == FtpStatus.Success)
                    {
                        Debug.WriteLine("Sucess send File!");
                    }
                }

            }
            client.Disconnect();
            return true;
        }

        public string GetUpdatelistFile(string patchVersion, string patchNum, bool isOffline = true)
        {
            string rootPatch = @"C:\PangYaSuiteTools";
            string ApplicationPath = rootPatch + @"\zip\" + patchVersion + @"\" + patchNum + @"\";
            if (isOffline == false)
            {
                string updatelistName = "updatelist";
                bool folderExists = Directory.Exists(ApplicationPath);
                if (!folderExists)
                    Directory.CreateDirectory(ApplicationPath);

                if (File.Exists(ApplicationPath + updatelistName))
                {
                    return ApplicationPath + @"updatelist";
                }
                else
                {
                    return ApplicationPath + @"updatelist";
                }
            }
            else
            {
                string updatelistName = "/updatelist";
                bool folderExists = Directory.Exists(ApplicationPath);
                if (!folderExists)
                    Directory.CreateDirectory(ApplicationPath);
                CheckConnection();
                if (client.FileExists(updatelistName))
                {
                    client.DownloadFile(ApplicationPath + "updatelist", updatelistName);
                    client.Disconnect();
                    return ApplicationPath + "updatelist";
                }
                else
                {
                    return "None";
                }
            }
        }
    }
}
