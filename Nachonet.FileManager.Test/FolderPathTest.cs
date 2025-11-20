using Microsoft.Extensions.Logging;
using Nachonet.FileManager.Configuration;
using Nachonet.FileManager.Data;
using Nachonet.FileManager.Errors;
using Nachonet.FileManager.Models;
using System.IO;

namespace Nachonet.FileManager.Test
{
    [TestClass]
    public class FolderPathTest
    {
        private const string RootName = "/virt/";
        private const string RootDir = "C:\\temp";
        private const StringComparison Cmp = StringComparison.CurrentCultureIgnoreCase;

        [TestMethod]
        public void TestInvalidRelativePath()
        {
            RootFolders rootFolders = new(new Dictionary<string, string>() { [RootName] = RootDir });
            Assert.Throws<FileManagerIoException>(delegate ()
            {
                FolderPath.ToRelativePath(new DirectoryInfo("C:\\invalid"), rootFolders, Cmp);
            });
        }

        [TestMethod]
        public void TestEquality()
        {
            var p1 = new FolderPath("/virt/path/", Cmp);
            var p2 = new FolderPath("/virt/path/", Cmp);
            FolderPath? p3 = null;
            FolderPath? p4 = null;
            bool areEqual12 = p1 == p2;
            bool areEqual34 = p3 == p4;
            bool areEqual13 = p1 == p3;
            bool areEqual31 = p3 == p1;
            Assert.IsTrue(areEqual12);
            Assert.IsTrue(areEqual34);
            Assert.IsFalse(areEqual13);
            Assert.IsFalse(areEqual31);
        }

        [TestMethod]
        public void TestSubDirRelativePath()
        {
            RootFolders rootFolders = new(new Dictionary<string, string>() { [RootName] = RootDir });
            var path = FolderPath.ToRelativePath(new DirectoryInfo("C:\\temp\\path"), rootFolders, Cmp);
            Assert.AreEqual(new FolderPath("/virt/path/", Cmp), path);
            Assert.AreEqual("/virt/path/", path.ToString());
        }

        [TestMethod]
        public void TestMRelativePath()
        {
            RootFolders rootFolders = new(new Dictionary<string, string>() { ["/M-Drive/"] = "M:\\" });
            var path = FolderPath.ToRelativePath(new DirectoryInfo("M:\\test\\"), rootFolders, Cmp);
            Assert.AreEqual(new FolderPath("/M-Drive/test/", Cmp), path);
            Assert.AreEqual("/M-Drive/test/", path.ToString());
        }

        [TestMethod]
        public void TestSubSubDirRelativePath()
        {
            RootFolders rootFolders = new(new Dictionary<string, string>() { [RootName] = RootDir });
            var path = FolderPath.ToRelativePath(new DirectoryInfo("C:\\temp\\path\\to\\folder"), rootFolders, Cmp);
            Assert.AreEqual(new FolderPath("/virt/path/to/folder/", Cmp), path);
            Assert.AreEqual("/virt/path/to/folder/", path.ToString());
        }

        [TestMethod]
        public void TestSubFileRelativePath()
        {
            RootFolders rootFolders = new(new Dictionary<string, string>() { [RootName] = RootDir });
            var path = FolderPath.ToRelativePath(new FileInfo("C:\\temp\\file.txt"), rootFolders, Cmp);
            Assert.AreEqual(new FolderPath("/virt/file.txt", Cmp), path);
            Assert.AreEqual("/virt/file.txt", path.ToString());
        }

        [TestMethod]
        public void TestSubSubFileRelativePath()
        {
            RootFolders rootFolders = new(new Dictionary<string, string>() { [RootName] = RootDir });
            var path = FolderPath.ToRelativePath(new FileInfo("C:\\temp\\path\\to\\file.txt"), rootFolders, Cmp);
            Assert.AreEqual(new FolderPath("/virt/path/to/file.txt", Cmp), path);
            Assert.AreEqual("/virt/path/to/file.txt", path.ToString());
        }

        [TestMethod]
        public void TestToDirectory()
        {
            RootFolders rootFolders = new(new Dictionary<string, string>() { [RootName] = RootDir });
            var path = FolderPath.ToRelativePath(new DirectoryInfo("C:\\temp\\path"), rootFolders, Cmp);
            var dir = path.ToDirectory(rootFolders);
            Assert.AreEqual("C:\\temp\\path\\", dir.FullName);
        }
    }
}