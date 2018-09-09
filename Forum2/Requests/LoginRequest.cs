using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Forum2.Requests
{
    public struct LoginRequest
    {
        public string username;
        public string password;
        public bool invalid;
    }
}
