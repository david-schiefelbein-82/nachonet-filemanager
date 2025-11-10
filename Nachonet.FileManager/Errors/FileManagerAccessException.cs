using System.Net;

namespace Nachonet.FileManager.Errors
{
    public class FileManagerAccessException : FileManagerException
    {
        public FileManagerAccessException(string? message) : base(message, HttpStatusCode.InternalServerError)
        {
        }

        public FileManagerAccessException(string? message, Exception? innerException) : base(message, HttpStatusCode.InternalServerError, innerException)
        {
        }
    }
}
