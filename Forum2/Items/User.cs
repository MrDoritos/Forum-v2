using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Forum2.Items
{
    public class User
    {
        public static User Anonymous { get; } = new User();
                        
        public User() { username = "anonymous"; password = ""; id = 0; }
        public User(String username, String password, int id) { this.username = username; this.password = password; this.id = id; currentToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray()).Replace("+", "m"); userThreads = new List<UserThread>(); }
        public User(String username, String password, int id, List<UserThread> userThreads, bool deleted) { this.username = username; this.password = password; this.id = id; this.userThreads = userThreads; currentToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray()).Replace("+", "m"); userThreads = new List<UserThread>(); }

        public List<UserThread> userThreads;
        public readonly string username;
        public readonly string password;
        public readonly int id;
        public string currentToken;
        private bool deleted;
        public UserThread.State Seen(Thread thread) { if (username == "anonymous") return UserThread.State.Read; var userThread = userThreads.FirstOrDefault(n => n.ThreadId == thread.id); if (userThread == null) return UserThread.State.New; var undeleted = thread.GetUndeletedComments(); if (undeleted.Count() > 0) if (userThread.LastSeen.Ticks > undeleted.Select(n => n.TimeStamp).Max().Ticks) return UserThread.State.Read; else return UserThread.State.Unread; else if (userThread.LastSeen.Ticks > thread.creationTime.Ticks) return UserThread.State.Read; return UserThread.State.Read; }
        public void Update(int id) { var userThread = userThreads.FirstOrDefault(n => n.ThreadId == id); if (userThread != null) userThread.Update(); else userThreads.Add(new UserThread(id)); }
        public bool IsDeleted { get { return deleted; } }
        public void Delete() { deleted = true; }

        public override string ToString()
        {
            return username;
        }

        public string ToString(string deletedtext)
        {
            if (IsDeleted) return $"{username} {deletedtext}"; else return username;
        }
    }
}
