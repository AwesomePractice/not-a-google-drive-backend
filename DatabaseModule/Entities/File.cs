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
        public string FileName;

        public int FileSize;

        public int FileType;

        public ObjectId OwnerId;

        public ObjectId FolderId;

        public string EncryptionKey;

        public int CompressionAlgorithm;

    }
}
