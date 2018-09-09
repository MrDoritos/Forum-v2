using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Forum2.Items;

namespace Forum2.Requests
{
    public class Request
    {
        public Request() { User = User.Anonymous; }
        public Request(User User) { this.User = User ?? User.Anonymous; }
        public User User;
    }
}
