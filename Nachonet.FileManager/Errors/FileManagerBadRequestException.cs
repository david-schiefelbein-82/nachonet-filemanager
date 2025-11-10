using System.Net;

namespace Nachonet.FileManager.Errors
{
    public class FileManagerBadRequestException : FileManagerException
    {
        public FileManagerBadRequestException(string? message) : base(message, HttpStatusCode.BadRequest)
        {
        }

        public FileManagerBadRequestException(string? message, Exception? innerException) : base(message, HttpStatusCode.BadRequest, innerException)
        {
        }
    }
}
