using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Forum2.Items
{
    struct UserStruct
    {
        public List<UserThread> userThreads;
        public string username;
        public string password;
        public int id;
        public string currentToken;
        public bool deleted;
    }
}
