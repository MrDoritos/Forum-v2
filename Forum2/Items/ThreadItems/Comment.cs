using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Forum2.Items.ThreadItems
{
    public class Comment
    {
        public Content Content;
        public String Text;
        public User Author;
        public DateTime TimeStamp;
        public Int32 Id;
        public Boolean Deleted;

        public Comment() { }
        public Comment(String Text, User Author, DateTime TimeStamp, Int32 Id) { this.Content = null; this.Author = Author ?? new User(); this.TimeStamp = TimeStamp; this.Id = Id; Deleted = false; this.Text = Text ?? ""; }
        public Comment(Content Content, User Author, DateTime TimeStamp, Int32 Id) { this.Content = Content; this.Author = Author; this.TimeStamp = TimeStamp; this.Id = Id; Deleted = false; }
    }
}
