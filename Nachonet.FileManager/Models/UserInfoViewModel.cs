using System;
using System.DirectoryServices.ActiveDirectory;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Identity;
using Nachonet.FileManager.Data;

namespace Nachonet.FileManager.Models
{
    public class UserInfoViewModel
    {
        /// <summary>
        /// if true the browser will issue console.log events for debugging
        /// </summary>
        public bool ClientDebugging { get; set; }

        public bool HasFileReaderRole { get; set; }

        public bool HasFileWriterRole { get; set; }

        public bool HasFileDownloaderRole { get; set; }

        public bool HasFileUploaderRole { get; set; }

        public FileManagerLayout Layout { get; set; }

        public int UploadChunkSize { get; set; }

        public UserInfoViewModel(bool clientDebugging, FileManagerLayout layout, int uploadChunkSize)
        {
            ClientDebugging = clientDebugging;
            Layout = layout;
            UploadChunkSize = uploadChunkSize;
        }

        public override string ToString()
        {
            var roles = new List<string>();
            if (HasFileReaderRole)
                roles.Add(FileManagerRoles.FileReader);
            
            if (HasFileWriterRole)
                roles.Add(FileManagerRoles.FileWriter);
            
            if (HasFileDownloaderRole)
                roles.Add(FileManagerRoles.FileDownloader);

            if (HasFileUploaderRole)
                roles.Add(FileManagerRoles.FileUploader);

            return string.Format("{{ Layout: {0}, roles: {0} }}", Layout, string.Join(", ", roles));
        }
    }
}