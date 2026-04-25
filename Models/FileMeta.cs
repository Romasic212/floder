using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace floder.Models
{
    public class FileMeta
    {
        public string Path { get; set; }
        public long Size { get; set; }
        public long LastModified { get; set; }
    }
}
