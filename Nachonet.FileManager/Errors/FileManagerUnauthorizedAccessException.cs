using System.Net;

namespace Nachonet.FileManager.Errors
{
    public class FileManagerUnauthorizedAccessException : FileManagerException
    {
        public FileManagerUnauthorizedAccessException(string? message) : base(message, HttpStatusCode.Unauthorized)
        {
        }

        public FileManagerUnauthorizedAccessException(string? message, Exception? innerException) : base(message, HttpStatusCode.Unauthorized, innerException)
        {
        }
    }
}
