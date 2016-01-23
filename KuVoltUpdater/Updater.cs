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
        bool isComplete;

        string applicationPath;
        string applicationDirectory;
        Uri applicationDirectoryUri;

        Dictionary<string, string> originChecksums;
        Dictionary<string, string> localChecksums;

        Task integrityCheckTask;
        Task updateTask;

        List<string> updateRequiredFiles = new List<string>();
        public Updater(MainWindow main, string url)
        {
            this.main = main;
            this.origin = new Uri(url);
            this.web = new WebClient();

            this.applicationPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            this.applicationDirectory = Path.GetDirectoryName(applicationPath) + "\\";
            this.applicationDirectoryUri = new Uri(applicationDirectory);

            web.DownloadProgressChanged += this.UpdateProgress;
        }
        public void IntegrityCheck()
        {
            this.integrityCheckTask = new Task(IntegrityCheckAction);
            integrityCheckTask.Start();
        }
        public void Update()
        {
            if (updateRequiredFiles.Count > 0 && !isComplete)
            {
                Update(false);
            }
            else
            {
                Update(true);
            }
        }
        public void ForcedUpdate()
        {
            Update(true);
        }
        void Update(bool isForced)
        {
            this.updateTask = new Task(() =>
            {
                UpdateAction(isForced);
            });
            this.updateTask.Start();
        }
        void UpdateAction(bool isForced)
        {
            try
            {
                main.updateButton.Dispatcher.Invoke(() =>
                {
                    main.updateButton.Content = "다운로드 중...";
                    main.updateButton.IsEnabled = false;
                });
                if (isForced)
                {
                    Logger.WriteLine("====강제 파일 다운로드 중...");
                    var files = originChecksums.Keys.ToList();

                    foreach (string filePath in files)
                    {
                        FileDownload(filePath, files.IndexOf(filePath) + 1, files.Count);
                    }
                }
                else
                {
                    Logger.WriteLine("====파일 다운로드 중...");
                    foreach (string filePath in updateRequiredFiles)
                    {
                        FileDownload(filePath, updateRequiredFiles.IndexOf(filePath) + 1, updateRequiredFiles.Count);
                    }
                }
                Logger.WriteLine("====다운로드 완료");
                this.isComplete = true;
                main.updateButton.Dispatcher.Invoke(() =>
                {
                    main.updateButton.Content = "다시 다운로드";
                    main.updateButton.IsEnabled = true;
                });
            }
            catch (Exception e)
            {
                Logger.WriteLine(e.ToString());
                main.UpdateButtonError();
            }
        }

        void FileDownload(string filePath, int currentIndex, int maxIndex)
        {
            this.main.downloadProgress.Dispatcher.InvokeAsync(() =>
            {
                this.main.downloadProgress.Value = 0;
            });
            // 디렉토리 체크
            string directory = Path.GetDirectoryName(filePath);
            if (directory != string.Empty && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            // 다운로드
            Uri originFile = new Uri(origin, filePath);
            RETRY:
            Logger.WriteLine("다운로드 파일 : {1}/{2} - {0}", filePath, currentIndex, maxIndex);
            Task downloadTask = web.DownloadFileTaskAsync(originFile, filePath);
            try
            {
                downloadTask.Wait();
            }
            catch (AggregateException e)
            {
                if (e.InnerException != null)
                {
                    if (e.InnerException.InnerException != null)
                    {
                        if (e.InnerException.InnerException.GetType() == typeof(IOException))
                        {
                            goto RETRY;
                        }
                    }
                    else
                    {
                        Logger.WriteLine(e.InnerException.ToString());
                    }
                }
                else
                {
                    throw e;
                }
            }
            this.main.downloadStatus.Dispatcher.InvokeAsync(() =>
            {
                this.main.downloadStatus.Text = "";
            });
        }
        void UpdateProgress(object sender, DownloadProgressChangedEventArgs e)
        {
            this.main.downloadProgress.Dispatcher.InvokeAsync(() =>
            {
                this.main.downloadProgress.Value = (double)e.BytesReceived / e.TotalBytesToReceive * 100;
                this.main.downloadStatus.Text = string.Format("{0:N0} KiB / {1:N0} KiB", e.BytesReceived / 1024, e.TotalBytesToReceive / 1024);
            });
        }
        void IntegrityCheckAction()
        {
            try
            {
                originChecksums = GetOriginChecksum();
                localChecksums = GetLocalChecksum(originChecksums.Keys.ToArray());

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
                if (updateRequiredFiles.Count > 0)
                {
                    main.updateButton.Dispatcher.Invoke(() =>
                    {
                        main.updateButton.Content = "다운로드";
                        main.updateButton.IsEnabled = true;
                    });
                }
                else
                {
                    main.updateButton.Dispatcher.Invoke(() =>
                    {
                        Logger.WriteLine("파일이 모두 일치합니다.");
                        main.updateButton.Content = "강제 다운로드";
                        main.updateButton.IsEnabled = true;
                    });
                }
                Logger.WriteLine();
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

            DirectoryInfo directory = new DirectoryInfo(".\\");
            var filelist = directory.GetFiles("*", SearchOption.AllDirectories);
            foreach (var fileInfo in filelist)
            {
                fileNameList.Add(fileInfo.FullName);
            }

            foreach (var filePath in fileNameList)
            {
                var fullPath = Path.GetFullPath(filePath);

                var relativePath = Uri.UnescapeDataString(applicationDirectoryUri.MakeRelativeUri(new Uri(fullPath)).ToString());

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
