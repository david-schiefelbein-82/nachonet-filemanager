using Nachonet.FileManager.Errors;
using System.Drawing.Text;
using System.IO;
using System.Runtime.InteropServices;

namespace Nachonet.FileManager.Data
{
    public class FileUpload(
        string uploadId,
        FileInfo tempFile,
        FolderPath folderId,
        RootFolders rootFolders,
        string fileName,
        long fileLen,
        int chunks,
        int chunkSize)
    {

        private readonly FileStream _fileStream = new (tempFile.FullName, FileMode.Create, FileAccess.Write, FileShare.Read);
        private readonly RootFolders _rootFolders = rootFolders;
        private readonly int[] _chunksUploaded = new int[chunks];

        public string UploadId { get; } = uploadId;

        public DateTime Started { get; } = DateTime.Now;

        public Int64 Updated { get; set; } = Environment.TickCount64;

        public FolderPath FolderId { get; } = folderId;

        public string FileName { get; } = fileName;

        public long FileSize { get; } = fileLen;

        public int Chunks { get; } = chunks;

        public int ChunkSize { get; } = chunkSize;

        public FileInfo TempFile { get; } = tempFile;

        public override string ToString()
        {
            return string.Format("{{ uploadId: {0}, folder: {1}, file: {2}, fileLen: {3} }}", UploadId, FolderId, FileName, FileSize);
        }

        public void Expire()
        {
            CloseStream();
            TempFile.Delete();
        }

        public void AddChunk(int chunkId, long fileOffset, byte[] data, int dataLen)
        {
            Updated = Environment.TickCount64;
            _chunksUploaded[chunkId] = dataLen;
            _fileStream.Seek(fileOffset, SeekOrigin.Begin);
            _fileStream.Write(data, 0, dataLen);
        }

        public void Cancel()
        {
            CloseStream();
            TempFile.Delete();
        }

        private long TotalBytesUploaded()
        {
            long total = 0;
            foreach (var chunkSize in _chunksUploaded)
            {
                total += chunkSize;
            }

            return total;
        }

        public void Complete()
        {
            // confirm that all chunks were uploaded... if not then failure
            var total = TotalBytesUploaded();
            if (total != FileSize)
            {
                throw new FileManagerIoException(string.Format("uploaded {0} bytes, expected {1} bytes", total, FileSize));
            }

            var dir = FolderId.ToDirectory(_rootFolders);
            var file = new FileInfo(Path.Join(dir.FullName, FileName)).Unique();

            CloseStream();
            TempFile.MoveTo(file.FullName);
        }

        private void CloseStream()
        {
            try
            {
                _fileStream.Flush();
            }
            catch (Exception)
            {
            }

            try
            {
                _fileStream.Close();
            }
            catch (Exception)
            {
            }
        }
    }
}
