namespace Nachonet.FileManager.Models
{
    public class FileUploadRequest
    {
        public string fileName { get; set; }

        public FileUploadRequest()
        {
            fileName = string.Empty;
        }
    }
}
