using System.Net;

namespace Nachonet.FileManager.Errors
{
    public class FileManagerIoException : FileManagerException
    {
        public FileManagerIoException(string? message) : base(message, HttpStatusCode.InternalServerError)
        {
        }

        public FileManagerIoException(string? message, Exception? innerException) : base(message, HttpStatusCode.InternalServerError, innerException)
        {
        }
    }
}
