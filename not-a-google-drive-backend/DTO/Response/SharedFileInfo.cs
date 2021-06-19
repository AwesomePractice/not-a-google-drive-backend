

using DatabaseModule.Entities;
using MongoDB.Bson;
using System.Collections.Generic;
using System.Linq;

namespace not_a_google_drive_backend.DTO.Response
{
    public class SharedFileInfo : UserFilesInfoFile
    {
        public SharedFileInfo(File file) : base(file)
        {
            AllowedUsers = file.AllowedUsers.Select(id => id.ToString()).ToList();
        }

        public List<string> AllowedUsers;
    }
}