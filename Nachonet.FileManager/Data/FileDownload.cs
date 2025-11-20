using System.Drawing;

namespace Nachonet.FileManager.Data
{
    public class FileDownload(string[] fileIds, string fileName, byte[] data)
    {
        public string[] FileIds { get; } = fileIds;

        public DateTime Started { get; } = DateTime.Now;

        public Int64 Updated { get; set; } = Environment.TickCount64;

        public string FileName { get; } = fileName;

        public byte[] Data { get; } = data;

        public long FileSize { get => Data.LongLength; }

        public string ETag
        {
            get
            {
                return string.Format("{0:x}-{1:x}", FileSize, Started.Ticks);
            }
        }

        public override string ToString()
        {
            return string.Format("{{ fileIds: {0}, fileName: {1}, fileLen: {2} }}", string.Join(", ", FileIds), FileName, Data.LongLength);
        }

        public long Read(byte[] data, long start, long bufferSize)
        {
            var len = Math.Min(bufferSize, Data.LongLength - start);

            Array.Copy(Data, start, data, 0L, len);
            return len;
        }

        public void Refresh()
        {
            Updated = Environment.TickCount64;
        }
    }
}
