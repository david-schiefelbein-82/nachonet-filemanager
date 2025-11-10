using System.ComponentModel;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Nachonet.FileManager.Models
{
    public class FileOperationResultViewModel
    {
        public string OperationId { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ClipboardResultCode Result { get; set; }

        public string Message { get; set; }

        public ClipboardResult[] Results { get; set; }

        public FileOperationResultViewModel(string operationId, ClipboardResultCode result, string message, ClipboardResult[] results)
        {
            OperationId = operationId;
            Result = result;
            Message = message;
            Results = results;
        }

        public FileOperationResultViewModel(ClipboardResultCode result, string message, ClipboardResult[] results)
        {
            OperationId = Guid.NewGuid().ToString();
            Result = result;
            Message = message;
            Results = results;
        }
    }


    public class ClipboardResult
    {
        public bool Success { get; set; }

        public string Message { get; set; }

        public ClipboardResult(bool success, string message)
        {
            Success = success;
            Message = message;
        }
    }

    public enum ClipboardResultCode
    {
        [EnumMember(Value = "in-progress")]
        InProgress,
        [EnumMember(Value = "success")]
        Success,
        [EnumMember(Value = "partial")]
        Partial,
        [Description("error")]
        [EnumMember(Value = "errorx")]
        Error,
    }
}
