using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EncodeLibrary
{
    public class Text
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public virtual User User { get; set; }
    }
}
