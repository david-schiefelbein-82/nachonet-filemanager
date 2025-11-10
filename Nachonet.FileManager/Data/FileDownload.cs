using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Drawing;

namespace Nachonet.FileManager.Data
{
    public class FileDownload
    {
        public string[] FileIds { get; }

        public DateTime Started { get; }

        public Int64 Updated { get; set; }

        public string FileName { get; }

        public byte[] Data { get; }

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

        public FileDownload(string[] fileIds, string fileName, byte[] data)
        {
            FileIds = fileIds;
            FileName = fileName;
            Started = DateTime.Now;
            Updated = Environment.TickCount64;
            Data = data;
        }
    }
}
