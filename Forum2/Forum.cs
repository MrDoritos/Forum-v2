using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Forum2.UserManagement;
using Forum2.ThreadManagement;
using Forum2.ContentManagement;

namespace Forum2
{
    public class Forum
    {
        public Forum() { UserManager = new UserManager(); ThreadManager = new ThreadManager(); ContentManager = new ContentManager(); }
        public Forum(string userdatabasename, string threaddatabasename) { UserManager = new UserManager(userdatabasename); ThreadManager = new ThreadManager(); ContentManager = new ContentManager(); }
        public UserManager UserManager;
        public ThreadManager ThreadManager;
        public ContentManager ContentManager;
    }
}
