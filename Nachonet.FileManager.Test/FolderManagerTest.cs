using Microsoft.Extensions.Logging;
using Nachonet.Common.Web.AppLocal.Config;
using Nachonet.Common.Web.Configuration;
using Nachonet.FileManager.Configuration;
using Nachonet.FileManager.Data;
using System.IO;

namespace Nachonet.FileManager.Test
{
    [TestClass]
    public class FolderManagerTest
    {
        private const string RootName = "/virt/";
        private const string RootDir = "C:\\temp";
        private const StringComparison Cmp = StringComparison.CurrentCultureIgnoreCase;

        [TestMethod]
        public void TestToDirectory()
        {
            var rootFolders = new RootFolders(new Dictionary<string, string>() { [RootName] = RootDir });
            var dirInfo = new FileInfo("C:\\temp\\path");
            var path = FolderPath.ToRelativePath(dirInfo, rootFolders, Cmp);
            var dir = path.ToDirectory(rootFolders);
            Assert.AreEqual("C:\\temp\\path", dir.FullName);
        }

        public class MockConfigManager : IConfigManager
        {
            public FileServerConfig FileServer { get; }

            public WebServerConfig WebServer { get; }

            public IAuthorizationConfig Authorization { get; }

            public UserPreferenceConfig UserPreference { get; }

            public IAppLocalConfig AppLocal => throw new NotImplementedException();

            public MockConfigManager(Dictionary<string, string> rootFolders)
            {
                FileServer = new FileServerConfig() { RootFolders = rootFolders, Comparison = StringComparison.CurrentCultureIgnoreCase };
                WebServer = new WebServerConfig();
                Authorization = new AuthorizationConfig();
                UserPreference = new UserPreferenceConfig();
            }

            public void SaveAppLocal()
            {
            }
        }

        //private static FolderManager CreateFolderManager(Dictionary<string, string> rootFolders)
        //{
        //    ILoggerFactory lf = new Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory();
        //    var cfgManager = new MockConfigManager(rootFolders);

        //    var folderManager = new FolderManager(lf.CreateLogger<FolderManager>(), cfgManager);
        //    return folderManager;
        //}
    }
}