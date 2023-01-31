using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZBand_EZTV
{
    public class LoginJsonObject
    {
        public string token { get; set; }
        public long expirationUTC { get; set; }
        public int secondsToExpire { get; set; }
        public List<string> roles { get; set; }
        public string userName { get; set; }
        public string userDisplayName { get; set; }
        public object userID { get; set; }
        public object groupIDs { get; set; }
    }

    public class LoginRequestBody
    { 
        public string username { get; set; }
        public string password { get; set; }    
    }
}
