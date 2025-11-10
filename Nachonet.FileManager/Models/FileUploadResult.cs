namespace Nachonet.FileManager.Models
{
    public enum FileUploadStatus
    {
        Started,
        Uploading,
        Complete,
        Cancelling,
        Cancelled,
    }

    public class FileUploadResult
    {
        public string Id { get; set; }

        public FileUploadStatus Status { get; set; }

        public FileUploadResult(string id, FileUploadStatus status)
        {
            Id = id;
            Status = status;
        }
    }
}
