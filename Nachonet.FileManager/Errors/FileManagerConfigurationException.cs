using System.Net;

namespace Nachonet.FileManager.Errors
{
    public class FileManagerConfigurationException : FileManagerException
    {
        public FileManagerConfigurationException(string? message) : base(message, System.Net.HttpStatusCode.InternalServerError)
        {
        }

        public FileManagerConfigurationException(string? message, Exception? innerException) : base(message, HttpStatusCode.InternalServerError, innerException)
        {
        }
    }
}
