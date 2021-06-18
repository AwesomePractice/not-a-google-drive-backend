using DatabaseModule.VO;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseModule.Entities
{
    public enum Provider
    {
        Google
    };

    [BsonCollection("buckets")]
    public class Bucket : Document
    {
        public Bucket() { }

        public string Name { get; set; }

        public Provider Provider { get; set; }

        public ObjectId? OwnerId { get; set; }

        public GoogleBucketConfigData BucketConfigData { get; set; }

    }
}
