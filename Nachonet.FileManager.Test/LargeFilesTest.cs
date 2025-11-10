using Microsoft.Extensions.Logging;
using Nachonet.FileManager.Configuration;
using Nachonet.FileManager.Data;
using Nachonet.FileManager.Models;
using System.IO;
using System.Net;

namespace Nachonet.FileManager.Test
{
    [TestClass]
    public class LargeFilesTest
    {

        // [TestMethod]
        public void CreateReallyBigFiles()
        {
            var rootDir = new DirectoryInfo("C:\\Dev\\ThirdParty\\Root\\Large Files");
            if (!rootDir.Exists)
            {
                rootDir.Create();
            }

            long kb = 1024;
            long mb = 1024 * kb;
            long gb = 1024 * mb;
            CreateRandomFile(new FileInfo(Path.Join(rootDir.FullName, "10M.txt")), 10 * mb);
            CreateRandomFile(new FileInfo(Path.Join(rootDir.FullName, "100M.txt")), 100 * mb);
            CreateRandomFile(new FileInfo(Path.Join(rootDir.FullName, "1GB.txt")), 1 * gb);
            CreateRandomFile(new FileInfo(Path.Join(rootDir.FullName, "4GB.txt")), 4 * gb);
        }

        private void CreateRandomFile(FileInfo file, long size)
        {
            if (!file.Exists)
            {
                using var fs = new FileStream(file.FullName, FileMode.Create, FileAccess.Write, FileShare.Read);

                var rand = new Random();
                for (long i = 0; i < size; ++i)
                    fs.WriteByte((byte)rand.Next(32, 126));

                fs.Flush();
                fs.Close();
            }
        }
    }
}