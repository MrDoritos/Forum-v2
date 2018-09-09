using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Forum2.Items.ThreadItems;

namespace Forum2.Items
{
    public class Thread : Header
    {
        public readonly User author;
        public readonly DateTime creationTime;
        public readonly int id;
        public bool _deleted;
        private List<Comment> Comments;
        public int NextId { get => GetNextId(); }

        public bool IsDeleted { get => _deleted; }

        public Thread() { }
        private Thread(String content, String title) : base(content, title) { }
        private Thread(User author, Header header) : base(header) { this.author = author ?? new User(); creationTime = DateTime.Now; Comments = new List<Comment>(); }
        public Thread(User author, Int32 id, Header header) : base(header) { this.author = author ?? new User(); creationTime = DateTime.Now; this.id = id; Comments = new List<Comment>(); }
        public Thread(User author, Int32 id, List<Comment> comments, Header header) : base(header) { this.author = author ?? new User(); creationTime = DateTime.Now; this.id = id; Comments = comments ?? new List<Comment>(); }
        public Thread(User author, Int32 id, Comment[] comments, Header header) : base(header) { this.author = author ?? new User(); creationTime = DateTime.Now; this.id = id; Comments = (comments ?? new Comment[0]).ToList(); }
        
        public void Delete() { _deleted = true; }

        public void AddComment(Comment comment)
        {
            if (comment == null) { return; }
            Comments.Add(comment);
        }

        private int GetNextId()
        {
            return Comments.Count + 1;
        }
        
        public Comment GetComment(int id)
        {
            return Comments.FirstOrDefault(n => n.Id == id);
        }

        public IEnumerable<Comment> GetComments()
        {
            return Comments;
        }

        public IEnumerable<Comment> GetUndeletedComments()
        {
            return Comments.Where(n => !n.Deleted);
        }

        public void DeleteComment(int id)
        {
            var c = GetComment(id);
            if (c != null) { c.Deleted = true; }
        }
    }
}
