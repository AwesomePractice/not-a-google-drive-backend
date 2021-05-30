using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace not_a_google_drive_backend.Models
{
    public class ListOfFiles
    {
        public IEnumerable<string> Filenames { get; set; }
    }
}
