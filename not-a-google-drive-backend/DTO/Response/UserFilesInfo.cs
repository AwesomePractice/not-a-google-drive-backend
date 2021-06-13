using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace not_a_google_drive_backend.DTO.Response
{
    public class UserFilesInfo
    {
        public ObjectId OwnerId;

        public UserFilesInfoFolder RootFolder; 
    }

    public class UserFilesInfoFolder
    {
        public string Name;

        public UserFilesInfoFile[] Files;

        public UserFilesInfoFolder[] Children;
    }

    public class UserFilesInfoFile
    {
        public ObjectId Id;

        public string Name;

        public int Size;

        public int Type;
    }
}
