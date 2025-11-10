using System.ComponentModel;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Nachonet.FileManager.Models
{
    public class FileOperationAsyncResultViewModel
    {
        public string OperationId { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ClipboardResultCode Result { get; set; }

        public string Title { get; set; }

        public string Message { get; set; }

        public ClipboardResult[] Results { get; set; }

        public FileOperationAsyncResultViewModel(string operationId, ClipboardResultCode result, string title, string message, ClipboardResult[] results)
        {
            OperationId = operationId;
            Result = result;
            Title = title;
            Message = message;
            Results = results;
        }
    }
}
