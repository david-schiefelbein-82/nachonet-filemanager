namespace Nachonet.FileManager.Models
{
    public class FileDownloadResult
    {
        public string FileName { get; set; }

        public byte[] Data { get; set; }

        public FileDownloadResult(string fileName, byte[] data)
        {
            FileName = fileName;
            Data = data;
        }
    }
}
