using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Text.RegularExpressions;

namespace KuVoltUpdater
{
    public class Updater
    {
        MainWindow main;

        Uri origin;
        WebClient web;
        Task integrityCheckTask;

        List<string> updateRequiredFiles = new List<string>();
        public Updater(MainWindow main, string url)
        {
            this.main = main;
            this.origin = new Uri(url);
            this.web = new WebClient();
            this.integrityCheckTask = new Task(IntegrityCheckAction);

        }
        public void IntegrityCheck()
        {
            integrityCheckTask.Start();
        }
        void IntegrityCheckAction()
        {
            try
            {
                Dictionary<string, string> originChecksums = GetOriginChecksum();
                Dictionary<string, string> localChecksums = GetLocalChecksum(originChecksums.Keys.ToArray());

                var a = originChecksums.Keys.ToArray();

                Logger.WriteLine("====무결성 검사 중...");
                foreach (var originChecksum in originChecksums)
                {
                    string localChecksum;
                    // 파일 존재 여부 확인
                    if (localChecksums.TryGetValue(originChecksum.Key, out localChecksum))
                    {
                        // 동일 파일 여부 확인
                        if (originChecksum.Value != localChecksum)
                        {
                            updateRequiredFiles.Add(originChecksum.Key);
                            Logger.WriteLine("일치하지 않음 : {0}", originChecksum.Key);
                        }
                    }
                    else
                    {
                        updateRequiredFiles.Add(originChecksum.Key);
                        Logger.WriteLine("존재하지 않음 : {0}", originChecksum.Key);
                    }
                }
                main.updateButton.Dispatcher.Invoke(() =>
                {
                    if (updateRequiredFiles.Count > 0)
                    {
                        main.updateButton.Content = "업데이트";
                        main.updateButton.IsEnabled = true;
                    }
                    else
                    {
                        main.updateButton.Content = "강제 업데이트";
                        main.updateButton.IsEnabled = true;
                    }
                });
            }
            catch (Exception e)
            {
                Logger.WriteLine(e.ToString());
                main.UpdateButtonError();
            }
        }
        Dictionary<string, string> GetLocalChecksum(string[] originFileList)
        {
            Logger.WriteLine("====로컬 파일 체크섬 생성 중...");
            MD5 MD5 = MD5.Create();

            List<string> fileNameList = new List<string>();
            Dictionary<string, string> checksumlist = new Dictionary<string, string>();
            string thisPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string thisDirectory = Path.GetDirectoryName(thisPath) + "\\";
            Uri thisDirectoryUri = new Uri(thisDirectory);

            DirectoryInfo directory = new DirectoryInfo(".\\");
            var filelist = directory.GetFiles("*", SearchOption.AllDirectories);
            foreach (var fileInfo in filelist)
            {
                fileNameList.Add(fileInfo.FullName);
            }

            foreach (var filePath in fileNameList)
            {
                var fullPath = Path.GetFullPath(filePath);

                var relativePath = Uri.UnescapeDataString(thisDirectoryUri.MakeRelativeUri(new Uri(fullPath)).ToString());

                foreach (string originFilePath in originFileList)
                {
                    if (relativePath == originFilePath)
                    {
                        byte[] fileMD5;
                        using (var file = File.OpenRead(fullPath))
                        {
                            fileMD5 = MD5.ComputeHash(file);
                        }
                        StringBuilder checksum = new StringBuilder();
                        string tempString = "";
                        //byte[] fileBytes = File.ReadAllBytes(relativePath.ToString());
                        foreach (var temp in fileMD5)
                        {
                            tempString = temp.ToString("x2");

                            checksum.Append(tempString);
                        }
                        checksumlist.Add(relativePath.ToString(), checksum.ToString());
                    }
                }
            }
            return checksumlist;
        }
        Dictionary<string, string> GetOriginChecksum()
        {
            Logger.WriteLine("====원본 파일 체크섬 다운로드 중...");
            Dictionary<string, string> checksumlist = new Dictionary<string, string>();

            var md5Checksum = web.DownloadData(new Uri(origin, "KuVolt.md5"));
            string md5ChecksumString = Encoding.UTF8.GetString(md5Checksum);

            string[] md5ChecksumStringLines = Regex.Split(md5ChecksumString, Environment.NewLine + "+");

            Regex doubleSpaceRegex = new Regex(@"\s\s");
            foreach (var md5ChecksumStringLine in md5ChecksumStringLines)
            {
                var temp = doubleSpaceRegex.Split(md5ChecksumStringLine);
                if (temp.Length > 1)
                {
                    checksumlist.Add(temp[1], temp[0]);
                }
            }

            return checksumlist;
        }
    }
}
