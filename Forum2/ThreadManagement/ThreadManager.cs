using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Forum2.Items;
using Forum2.Items.ThreadItems;

namespace Forum2.ThreadManagement
{
    public class ThreadManager
    {
        private List<Thread> _threads;
        public int NextId { get => GetNextId(); }
        public ThreadManager() { _threads = new List<Thread>(); }

        public IEnumerable<Thread> GetThreads() { return _threads; }

        public void AddThread(Thread newThread)
        {
            _threads.Add(newThread);
        }

        public void AddThread(User author, Items.ThreadItems.Header header)
        {
            _threads.Add(new Thread(author, NextId, header));
        }

        public void AddComment(Thread thread, Comment comment)
        {
            if (comment != null) (thread ?? new Thread()).AddComment(comment);
        }

        public void AddComment(Thread thread, User sender, string Text)
        {
            thread.AddComment(new Comment(Text, sender, DateTime.Now, thread.NextId));
        }

        public void AddComment(int id, Comment comment)
        {
            var a = GetThread(id);
            if (a != null)
            {
                a.AddComment(comment);
            }
        }

        public void Delete(Thread thread)
        {
            (thread ?? new Thread()).Delete();
        }

        public void Delete(int id)
        {
            (GetThread(id) ?? new Thread()).Delete();
        }

        public Thread GetThread(int id)
        {
            return _threads.FirstOrDefault(n => n.id == id);
        }

        private int GetNextId()
        {
            return _threads.Count + 1;
        }
    }
}
