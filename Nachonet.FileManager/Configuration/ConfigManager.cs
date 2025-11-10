using System.Text.Json.Serialization;
using System.Text.Json;
using Nachonet.Common.Web.Configuration;
using Nachonet.Common.Web.AppLocal.Config;
using Nachonet.Common.Web.AppLocal;

namespace Nachonet.FileManager.Configuration
{
    public class ConfigManager : IConfigManager
    {
        public static JsonSerializerOptions SerializationOptions => new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true,
            Converters = { new JsonStringEnumConverter() },
        };

        private static readonly JsonSerializerOptions _toStringOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() },
        };

        [JsonPropertyName("file-server")]
        public FileServerConfig FileServer { get; private set; }

        [JsonPropertyName("web-server")]
        public WebServerConfig WebServer { get; private set; }

        [JsonPropertyName("authorisation")]
        public IAuthorizationConfig Authorization { get; private set; }

        [JsonPropertyName("local-users")]
        public IAppLocalConfig AppLocal { get; private set; }

        [JsonPropertyName("user-preferences")]
        public UserPreferenceConfig UserPreference { get; private set; }

        public ConfigManager()
        {
            FileServer = FileServerConfig.Load();
            WebServer = WebServerConfig.Load();
            Authorization = AuthorizationConfig.Load();
            UserPreference = UserPreferenceConfig.Load();
            AppLocal = LoadAppLocal(out bool appLocalModified);
            if (appLocalModified)
                SaveAppLocal();
        }

        public void SaveAppLocal()
        {
            foreach (AppLocalUserConfig user in AppLocal.Users.Cast<AppLocalUserConfig>())
            {
                // hash any user passwords
                if (string.IsNullOrEmpty(user.PasswordSalt) && !string.IsNullOrWhiteSpace(user.Password))
                {
                    user.PasswordSalt = AppLocalUserAuthenticator.GenerateNewSalt();
                    user.Password = AppLocalUserAuthenticator.HashPasword(user.Password, user.PasswordSalt);
                }
            }

            var configFile = Path.Combine("Config", "local-users.json");
            using var utf8Json = new FileStream(configFile, FileMode.Create, FileAccess.Write, FileShare.Read);
            JsonSerializer.Serialize(utf8Json, AppLocal, SerializationOptions);
            utf8Json.Close();
        }

        public static AppLocalConfig LoadAppLocal(out bool modified)
        {
            var configFile = Path.Combine("Config", "local-users.json");
            var path = Path.GetFullPath(configFile);
            var json = File.ReadAllText(path);
            var appConfig = JsonSerializer.Deserialize<AppLocalConfig>(json, SerializationOptions)
                ?? throw new AuthorizationConfigException("unable to load config file");

            modified = false;
            foreach (AppLocalUserConfig user in appConfig.Users.Cast<AppLocalUserConfig>())
            {
                // hash any user passwords
                if (string.IsNullOrEmpty(user.PasswordSalt) && !string.IsNullOrWhiteSpace(user.Password))
                {
                    user.PasswordSalt = AppLocalUserAuthenticator.GenerateNewSalt();
                    user.Password = AppLocalUserAuthenticator.HashPasword(user.Password, user.PasswordSalt);
                    modified = true;
                }
            }

            return appConfig;
        }

        public override string ToString()
        {
            return JsonSerializer.Serialize(this, _toStringOptions);
        }
    }
}
