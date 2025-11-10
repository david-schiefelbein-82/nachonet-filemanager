using Nachonet.Common.Web.AppLocal.Config;
using Nachonet.Common.Web.Configuration;

namespace Nachonet.FileManager.Configuration
{
    public interface IConfigManager
    {
        FileServerConfig FileServer { get; }

        WebServerConfig WebServer { get; }

        IAppLocalConfig AppLocal { get; }

        IAuthorizationConfig Authorization { get; }

        UserPreferenceConfig UserPreference { get; }

        void SaveAppLocal();
    }
}