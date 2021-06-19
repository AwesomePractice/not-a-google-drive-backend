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

        public string FileName { get; set; }

        public long FileSize { get; set; }

        public string FileType { get; set; }

        public ObjectId OwnerId { get; set; }

        public ObjectId FolderId { get; set; }

        public ObjectId BucketId { get; set; }

        public bool Encrypted { get; set; }

        public string EncryptionKey { get; set; }
        public string IV { get; set; }

        public bool Compressed { get; set; }

        public bool Favourite { get; set; }

    }
}
