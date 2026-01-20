using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EncodeLibrary
{
    public class History
    {
        public int Id { get; set; }
        public string Operation { get; set; }
        public DateTime Date { get; set; }
        public string Details { get; set; }
        public virtual User User { get; set; }
    }
}
