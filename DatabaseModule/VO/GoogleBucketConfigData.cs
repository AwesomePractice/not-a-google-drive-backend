using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseModule.VO
{
    public class GoogleBucketConfigData : Document
    {
        public string ClientId { get; set; }

        public string Secret { get; set; }

        public string Email { get; set; }

        public string ProjectId { get; set; }

        public string SelectedBucket { get; set; }
    }
}
