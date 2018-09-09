using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Forum2.Items;

namespace Forum2.UserManagement
{
    public class UserManager
    {
        private List<User> _users;
        
        public int NextId { get => GetNextId(); }

        public UserManager() { _users = new List<User>() { new User("anonymous", "", 1) }; }

        private void AddUser(User user)
        {
            if (user != null && !Exists(user.id)) { _users.Add(user); }
        }

        public User AddUser(string username, string password)
        {
            var user = new User(username, password, NextId);
            _users.Add(user);
            return user;
        }

        public void DeleteUser(User user)
        {
            user.Delete();
        }

        public int GetNextId()
        {
            return (_users.Count + 1);
        }

        public bool Exists(User user)
        {
            return (_users.Any(user.Equals));
        }
        
        public bool Exists(string username)
        {
            return (_users.Any(n => n.username == username));
        }

        public bool Exists(string username, string password)
        {
            return (_users.Any(n => n.username == username && n.password == password));
        }

        public bool Exists(int id)
        {
            return (_users.Any(n => n.id == id));
        }

        public bool TokenExists(string token)
        {
            return (_users.Any(n => n.currentToken == token));
        }

        public User GetUser(string username)
        {
            return (_users.FirstOrDefault(n => n.username == username));
        }

        public User GetUser(string username, string password)
        {
            return (_users.FirstOrDefault(n => n.username == username && n.password == password));
        }

        public User GetUser(int id)
        {
            return (_users.FirstOrDefault(n => n.id == id));
        }
        
        public string GetToken(User user)
        {
            return user.currentToken;
        }

        public User GetUserByToken(string token)
        {
            return _users.FirstOrDefault(n => n.currentToken == token);
        }        

        public string RenewToken(User user)
        {
            return (user.currentToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray()).Replace("+", "m"));
        }

        public User TryAuth(string username, string password)
        {
            if (Exists(username, password)) { return GetUser(username, password); } else return new User();
        }

        public User TryAuth(string token)
        {
            if (token == null || token == "") return User.Anonymous;

            if (TokenExists(token)) { return GetUserByToken(token); }
            return User.Anonymous;
        }
    }
}
