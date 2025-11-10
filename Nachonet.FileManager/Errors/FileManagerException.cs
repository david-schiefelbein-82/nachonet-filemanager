using Microsoft.AspNetCore.Http;
using System.Net;

namespace Nachonet.FileManager.Errors
{
    public class FileManagerException : Exception
    {
        public FileManagerException(string? message, HttpStatusCode statusCode) : base(message)
        {
            StatusCode = statusCode;
        }

        public FileManagerException(string? message, HttpStatusCode statusCode, Exception? innerException) : base(message, innerException)
        {
            StatusCode = statusCode;
        }

        public HttpStatusCode StatusCode { get; }
    }
}
