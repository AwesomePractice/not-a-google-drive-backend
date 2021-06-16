using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseModule.Entities
{
    [BsonCollection("folders")]
    public class Folder : Document
    {

        public Folder(string name, ObjectId ownerId, ObjectId[] children)
        {
            Name = name;
            OwnerId = ownerId;
            Children = children;
        }

        public Folder()
        {
        }

        public string Name;

        public ObjectId OwnerId;

        public ObjectId[] Children;
    }
}
