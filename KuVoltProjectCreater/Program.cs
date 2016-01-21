using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace KuVoltProjectCreater
{
    class Program
    {
        readonly static MD5 MD5 = MD5.Create();

        const string blacklistPath = "blacklist.txt";
        const string KuVoltMd5Path = "KuVolt.md5";

        static string[] blacklist;
        static List<string> fileNameList = new List<string>();
        static StringBuilder checksumlist = new StringBuilder();
        readonly static string thisPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
        readonly static string thisDirectory = Path.GetDirectoryName(thisPath) + "\\";
        readonly static Uri thisDirectoryUri = new Uri(thisDirectory);
        static void Main(string[] args)
        {
            try
            {
                blacklist = File.ReadAllLines("blacklist.txt");
            }
            catch (System.IO.FileNotFoundException)
            {
                blacklist = new string[0];
            }

            DirectoryInfo directory = new DirectoryInfo(".\\");
            var filelist = directory.GetFiles("*", SearchOption.AllDirectories);
            foreach (var fileInfo in filelist)
            {
                fileNameList.Add(fileInfo.FullName);
            }

            foreach (var filePath in fileNameList)
            {
                var fullPath = Path.GetFullPath(filePath);
                bool blocked = fullPath == thisPath;

                if (!blocked)
                {
                    var blacklistFullPath = Path.GetFullPath(blacklistPath);
                    blocked |= fullPath == blacklistFullPath;

                    var KuVoltMd5FullPath = Path.GetFullPath(KuVoltMd5Path);
                    blocked |= fullPath == KuVoltMd5FullPath;

                    foreach (var black in blacklist)
                    {
                        string blackFullPath = Path.GetFullPath(black);
                        blocked |= fullPath == blackFullPath;
                    }
                }
                if (!blocked)
                {
                    var relativePath = Uri.UnescapeDataString(thisDirectoryUri.MakeRelativeUri(new Uri(fullPath)).ToString());

                    byte[] fileMD5;
                    using (var file = File.OpenRead(fullPath))
                    {
                        Console.WriteLine("파일 : {0}", relativePath);
                        fileMD5 = MD5.ComputeHash(file);
                    }
                    foreach (var temp in fileMD5)
                    {
                        string tempString = temp.ToString("x2");
                        Console.Write(tempString);
                        checksumlist.Append(tempString);
                    }
                    Console.WriteLine();
                    Console.WriteLine();
                    checksumlist.Append("  ");

                    checksumlist.AppendLine(relativePath.ToString());
                }
            }
            File.WriteAllText(KuVoltMd5Path, checksumlist.ToString());
        }
    }
}
