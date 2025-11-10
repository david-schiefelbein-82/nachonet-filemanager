using Nachonet.FileManager.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Nachonet.FileManager.Configuration
{
    public class UserPreferenceConfig
    {
        [JsonPropertyName("users")]
        public List<UserSettings> Users { get; set; }

        public UserPreferenceConfig()
        {
            Users = new List<UserSettings>();
        }

        public static UserPreferenceConfig Load()
        {
            UserPreferenceConfig? config = null;
            try
            {
                var configFilePath = Path.Combine("Config", "user-preference.json");
                using var fileStream = new FileStream(configFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                config = JsonSerializer.Deserialize<UserPreferenceConfig>(fileStream, ConfigManager.SerializationOptions) ??
                    new UserPreferenceConfig();

                return config;
            }
            catch { }

            return config ?? new UserPreferenceConfig();
        }

        public override string ToString()
        {
            return JsonSerializer.Serialize(this, new JsonSerializerOptions()
            {
                WriteIndented = false,
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() },
            });
        }

        public bool TrySave()
        {
            try
            {
                var configFilePath = Path.Combine("Config", "user-preference.json");
                using var fileStream = new FileStream(configFilePath, FileMode.Create, FileAccess.Write, FileShare.Read);
                JsonSerializer.Serialize<UserPreferenceConfig>(fileStream, this, ConfigManager.SerializationOptions);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
    public class UserSettings
    {
        [JsonPropertyName("UserId")]
        public string UserId { get; set; }

        [JsonPropertyName("layout")]
        public FileManagerLayout Layout { get; set; }

        public UserSettings()
        {
            UserId = string.Empty;
            Layout = FileManagerLayout.Tiles;
        }

        public void CopyTo(UserSettings user)
        {
            user.Layout = Layout;
            user.UserId = UserId;
        }
    }
}
