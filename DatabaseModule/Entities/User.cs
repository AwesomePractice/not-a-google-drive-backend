using DatabaseModule.VO;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace DatabaseModule.Entities
{
    [BsonCollection("users")]
    public class User : Document
    {
        public string Login { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }

        #region External storage services data

        public ObjectId[] Buckets { get; set; }

        public Bucket CurrentBucket { get; set; }

        #endregion

        [BsonDateTimeOptions(DateOnly = true)]
        public DateTime BirthDate { get; set; }
        public string PasswordHash { get; set; }
        public string PasswordSalt { get; set; }
    }
}
