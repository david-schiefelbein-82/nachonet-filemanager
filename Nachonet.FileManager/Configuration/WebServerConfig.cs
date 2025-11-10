using Nachonet.Common.Web.ActiveDirectory.Config;
using Nachonet.Common.Web.Configuration;
using Nachonet.Common.Web.Oidc.Config;
using Nachonet.FileManager.Errors;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Nachonet.FileManager.Configuration
{
    [Flags]
    public enum WebServerLoginType
    {
        None = 0,
        Oidc = 1,
        ActiveDirectory = 2,
        LocalUser = 4,
    }

    public class WebServerConfig
    {
        private static readonly JsonSerializerOptions _toStringOptions = new()
        {
            WriteIndented = false,
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() },
        };

        [JsonPropertyName("client-debugging")]
        public bool ClientDebugging { get; set; }

        [JsonPropertyName("use-https-redirection")]
        public bool UseHttpsRedirection { get; set; }

        [JsonPropertyName("session-idle-timeout")]
        public TimeSpan SessionIdleTimeout { get; set; }

        [JsonPropertyName("path-base")]
        public string? PathBase { get; set; }

        [JsonPropertyName("login-method")]
        public WebServerLoginType LoginMethod { get; set; }

        [JsonPropertyName("ad-login-text")]
        public string AdLoginText { get; set; }

        [JsonPropertyName("oidc-login-text")]
        public string OidcLoginText { get; set; }


        [JsonPropertyName("jwt-token-authentication")]
        public JwtTokenAuthenticationConfig JwtTokenAuthentication { get; set; }

        [JsonPropertyName("sister-sites")]
        public SisterSiteConfig[] SisterSites { get; set; }

        [JsonPropertyName("oidc")]
        public OidcConfig Oidc { get; set; }

        [JsonPropertyName("active-directory")]
        public ActiveDirectoryConfig ActiveDirectory { get; set; }

        public WebServerConfig()
        {
            UseHttpsRedirection = true;
            SessionIdleTimeout = TimeSpan.FromMinutes(60);
            OidcLoginText = "Login";
            AdLoginText = "Login";
            SisterSites = [];
            Oidc = new OidcConfig();
            ActiveDirectory = new ActiveDirectoryConfig();
            JwtTokenAuthentication = new JwtTokenAuthenticationConfig();
        }

        public static WebServerConfig Load()
        {
            var configFilePath = Path.Combine("Config", "web-server.json");
            using var fileStream = new FileStream(configFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var config = JsonSerializer.Deserialize<WebServerConfig>(fileStream, ConfigManager.SerializationOptions) ??
                throw new FileManagerConfigurationException("unable to load config file");

            if (string.IsNullOrWhiteSpace(config.JwtTokenAuthentication.TokenKey))
            {
                config.JwtTokenAuthentication.TokenKey = JwtTokenAuthenticationConfig.GenerateKey(256);
            }

            return config;
        }

        public override string ToString()
        {
            return JsonSerializer.Serialize(this, _toStringOptions);
        }
    }
}
