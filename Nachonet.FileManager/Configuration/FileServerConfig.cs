using Nachonet.FileManager.Errors;
using Nachonet.FileManager.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Nachonet.FileManager.Configuration
{

    public class FileTypeConfig
    {
        [JsonPropertyName("type")]
        public FileType FileType { get; set; }

        [JsonPropertyName("icon")]
        public string? FileIcon{ get; set; }

        [JsonPropertyName("syntax")]
        public string Syntax { get; set; }

        [JsonPropertyName("read-only")]
        public bool IsReadOnly { get; set; }

        [JsonPropertyName("max-size")]
        public long MaxSize { get; set; }

        public FileTypeConfig()
        {
            FileType = FileType.Unknown;
            Syntax = "text";
            IsReadOnly = true;
            MaxSize = 1024 * 1024;
        }

        public override string ToString()
        {
            if (FileType == FileType.Text)
            {
                return string.Format("{0}/{1}{2}", FileType, Syntax, IsReadOnly ? " readonly" : "");
            } else
            {
                return string.Format("{0}{1}", FileType, IsReadOnly ? " readonly" : "");
            }
        }
    }

    public class FileServerConfig
    {
        private static readonly JsonSerializerOptions _toStringOptions = new()
        {
            WriteIndented = false,
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() },
        };

        [JsonPropertyName("root-folders")]
        public Dictionary<string, string> RootFolders { get; set; }

        [JsonPropertyName("upload-chunk-size")]
        public int UploadChunkSize { get; set; }

        [JsonPropertyName("upload-directory")]
        public string UploadDirectory { get; set; }

        [JsonPropertyName("use-https-redirection")]
        public bool UseHttpsRedirection { get; set; }

        [JsonPropertyName("comparison")]
        public StringComparison Comparison { get; set; }

        [JsonPropertyName("file-types")]
        public Dictionary<string, FileTypeConfig> FileTypes { get; set; }

        public FileServerConfig()
        {
            RootFolders = [];
            UploadDirectory = string.Empty;
            Comparison = StringComparison.CurrentCultureIgnoreCase;
            UseHttpsRedirection = true;
            UploadChunkSize = 1024 * 500;
            FileTypes = [ ];
        }

        public static FileServerConfig Load()
        {
            var configPath = Path.Combine("Config", "file-server.json");
            using var fileStream = new FileStream(configPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var config = JsonSerializer.Deserialize<FileServerConfig>(fileStream, ConfigManager.SerializationOptions) ??
                throw new FileManagerConfigurationException("unable to load config file");
            return config;
        }

        public override string ToString()
        {
            return JsonSerializer.Serialize(this, _toStringOptions);
        }


        public FileTypeConfig GetFileType(FileInfo file)
        {
            var extn = file.Extension.ToLower();
            if (FileTypes.TryGetValue(extn, out var type))
                return type;
            else
                return new FileTypeConfig() { FileType = FileType.Unknown, IsReadOnly = true, Syntax = string.Empty };
        }
    }
}
