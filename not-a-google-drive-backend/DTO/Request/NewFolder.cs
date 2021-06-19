using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace not_a_google_drive_backend.DTO.Request
{
    public class CreateFolder
    {
        public string ParentId { get; set; }

        public string Name { get; set; }

        public bool IsFavourite { get; set; }
    }
}
