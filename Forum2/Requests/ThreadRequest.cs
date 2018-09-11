using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Forum2.Items;
using Forum2.Items.ThreadItems;

namespace Forum2.Requests
{
    public class ThreadRequest : Request
    {
        public RequestTypes Type;
        public Thread RequestThread;
        public Header Header;
        public String Text;

        public ThreadRequest(RequestTypes type) : base() { Type = type; }
        public ThreadRequest(RequestTypes type, User user) : base(user) { Type = type; }
        public ThreadRequest(RequestTypes types, Thread thread, User user) : base(user) { Type = types; RequestThread = thread; }
        public ThreadRequest(RequestTypes types, Header header) : base() { Type = types; Header = header; }
        public ThreadRequest(RequestTypes types, Header header, User user) : base(user) { Type = types; Header = header; }
        public ThreadRequest(RequestTypes types, Thread thread, String Text) : base() { Type = types; this.Text = Text; RequestThread = thread; }
        public ThreadRequest(RequestTypes types, Thread thread, String Text, User user) : base(user) { Type = types; this.Text = Text; RequestThread = thread; }

        public enum RequestTypes
        {
            AddComment = 0,
            AddThread = 1,
            DeleteComment = 2,
            DeleteThread = 3,
            View = 4,
            Invalid = 5,
            InvalidComment = 6,
        }
    }
}
