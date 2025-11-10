using System.Net;

namespace Nachonet.FileManager.Errors
{
    public class FileManagerClipboardException : FileManagerException
    {
        public FileManagerClipboardException(string? message) : base(message, System.Net.HttpStatusCode.InternalServerError)
        {
        }

        public FileManagerClipboardException(string? message, Exception? innerException) : base(message, HttpStatusCode.InternalServerError, innerException)
        {
        }
    }
}
