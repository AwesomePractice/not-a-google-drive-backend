using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseModule.Entities
{
    [BsonCollection("files")]
    public class File : Document
    {
        public File() { }

        public string FileName;

        public long FileSize;

        public string FileType;

        public ObjectId OwnerId;

        public ObjectId FolderId;

        public ObjectId BucketId;

        public List<ObjectId> AllowedUsers;

        //public string EncryptionKey;

        //public int CompressionAlgorithm;

        public bool Encrypted;

        public bool Compressed;

        public bool Favourite;

    }
}
