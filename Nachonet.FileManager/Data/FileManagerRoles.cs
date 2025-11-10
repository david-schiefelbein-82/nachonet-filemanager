using Nachonet.FileManager.Errors;
using System.Security.Claims;

namespace Nachonet.FileManager.Data
{
    public static class FileManagerRoles
    {
        public const string FileReader = "file-reader";
        public const string FileDownloader = "file-downloader";
        public const string FileUploader = "file-uploader";
        public const string FileWriter = "file-writer";
        public const string Admin = "admin";

        /// <summary>
        /// asserts that the current user has permission to read, and download files
        /// </summary>
        /// <exception cref="FileManagerAccessException">if the user does not have permission</exception>
        public static void AssertFileReader(this ClaimsPrincipal user)
        {
            if (user.Identity == null || !user.Identity.IsAuthenticated)
            {
                throw new FileManagerAccessException("user is not logged in");
            }

            if (user.IsInRole(FileManagerRoles.FileReader))
                return;

            throw new FileManagerAccessException("user does not have permission to read files");
        }

        /// <summary>
        /// asserts that the current user has permission to download files
        /// </summary>
        /// <exception cref="FileManagerAccessException">if the user does not have permission</exception>
        public static void AssertFileDownloader(this ClaimsPrincipal user)
        {
            if (user.Identity == null || !user.Identity.IsAuthenticated)
            {
                throw new FileManagerAccessException("user is not logged in");
            }

            if (user.IsInRole(FileManagerRoles.FileDownloader))
                return;

            throw new FileManagerAccessException("user does not have permission to download files");
        }

        /// <summary>
        /// asserts that the current user has permission to upload files
        /// </summary>
        /// <exception cref="FileManagerAccessException">if the user does not have permission</exception>
        public static void AssertFileUploader(this ClaimsPrincipal user)
        {
            if (user.Identity == null || !user.Identity.IsAuthenticated)
            {
                throw new FileManagerAccessException("user is not logged in");
            }

            if (user.IsInRole(FileManagerRoles.FileUploader))
                return;

            throw new FileManagerAccessException("user does not have permission to upload files");
        }

        /// <summary>
        /// asserts that the current user has permission to create, write and modify files and directories
        /// </summary>
        /// <exception cref="FileManagerAccessException">if the user does not have permission</exception>
        public static void AssertFileWriter(this ClaimsPrincipal user)
        {
            if (user.Identity == null || !user.Identity.IsAuthenticated)
            {
                throw new FileManagerAccessException("user is not logged in");
            }

            if (user.IsInRole(FileManagerRoles.FileWriter))
                return;

            throw new FileManagerAccessException("user does not have permission to modify files");
        }
        /// <summary>
        /// asserts that the current user has admin permissions
        /// </summary>
        /// <exception cref="FileManagerAccessException">if the user does not have permission</exception>
        public static void AssertAdmin(this ClaimsPrincipal user)
        {
            if (user.Identity == null || !user.Identity.IsAuthenticated)
            {
                throw new FileManagerAccessException("user is not logged in");
            }

            if (user.IsInRole(FileManagerRoles.Admin))
                return;

            throw new FileManagerAccessException("user does not have admin permissions");
        }

    }
}
