using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Text;

namespace DatabaseModule
{
    public abstract class Document : IDocument
    {
        public ObjectId Id { get; set; }

        public DateTime CreatedAt => Id.CreationTime;
    }
}
