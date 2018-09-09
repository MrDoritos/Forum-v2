using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Forum2.Items
{
    public class UserThread
    {
        public UserThread(int id) { ThreadId = id; }
        public int ThreadId { get; private set; }
        public DateTime LastSeen { get; private set; } = DateTime.Now;
        public void Update() { LastSeen = DateTime.Now; }
        public enum State
        {
            New,
            Unread,
            Read
        }
    }
}
