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
        public string Name;

        public ObjectId OwnerId;

        public Folder[] Children;
    }
}
