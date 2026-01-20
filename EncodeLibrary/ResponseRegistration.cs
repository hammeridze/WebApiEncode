using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EncodeLibrary
{
    public class ResponseRegistration
    {
        public int Id { get; set; }
        public string JwtToken { get; set; }
        public DateTime Expiration { get; set; }
        public bool Success { get; set; }
        public string Note { get; set; }
        public User User { get; set; }
    }
}
