using System.Text.Json.Serialization;

namespace Nachonet.FileManager.Models
{
    public class SetFileContentsRequest
    {
        [JsonPropertyName("fileId")]
        public string? FileId { get; set; }

        [JsonPropertyName("content")]
        public string? Content { get; set; }

        public SetFileContentsRequest()
        {
        }
    }
}
