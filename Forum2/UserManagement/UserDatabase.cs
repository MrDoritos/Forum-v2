using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.IO;
using Forum2;
using Forum2.Items;

namespace Forum2.UserManagement
{
    public class UserDatabase : List<User>
    {
        public UserDatabase() : base() { }
        public UserDatabase(List<User> users) : base(users) { }
        public UserDatabase(IEnumerable<User> users) : base(users) { }

        public void Save()
        {
            SaveDatabase("users.xml");
        }

        public void Save(string filename)
        {
            SaveDatabase(filename);
        }

        private void SaveDatabase(string filename)
        {
            XmlDocument xml = new XmlDocument();

            XmlDeclaration xmlDeclaration = xml.CreateXmlDeclaration("1.0", "UTF-8", null);
            XmlElement root = xml.DocumentElement;
            xml.InsertBefore(xmlDeclaration, root);

            XmlElement users = xml.CreateElement("users");
            xml.AppendChild(users);

            foreach (User user in base.ToArray().Where(n => n.id != 0))
                users.AppendChild(GetUser(user, xml));

            xml.Save(filename);

        }

        private XmlNode GetUser(User user, XmlDocument xml)
        {
            XmlElement userNode = xml.CreateElement("user");

            userNode.AppendChild(xml.CreateElement("username")).InnerText = user.username;

            userNode.AppendChild(xml.CreateElement("password")).InnerText = user.password;

            userNode.AppendChild(xml.CreateElement("deleted")).InnerText = user.IsDeleted.ToString();

            userNode.AppendChild(xml.CreateElement("id")).InnerText = user.id.ToString();

            XmlElement userThreads = xml.CreateElement("userThreads");

            foreach (UserThread userThread in user.userThreads)
                userThreads.AppendChild(GetUserThread(userThread, xml));

            return userNode;
        }

        private XmlNode GetUserThread(UserThread userThread, XmlDocument xml)
        {
            XmlElement userThreadNode = xml.CreateElement("userThread");

            userThreadNode.AppendChild(xml.CreateElement("threadId")).InnerText = userThread.ThreadId.ToString();

            userThreadNode.AppendChild(xml.CreateElement("lastSeen")).InnerText = userThread.LastSeen.ToString();

            return userThreadNode;

        }

        public static UserDatabase Parse(string filename)
        {
            XmlDocument xml = new XmlDocument();
            xml.Load(filename);

            List<User> parsedUsers = new List<User>();

            foreach (XmlNode node in xml.SelectSingleNode("//users").ChildNodes)
            {
                UserStruct user = new UserStruct();

                user.username = node["username"].InnerText;
                user.password = node["password"].InnerText;
                user.deleted = bool.Parse(node["deleted"].InnerText);
                user.id = int.Parse(node["id"].InnerText);

                List<UserThread> userThreads = new List<UserThread>();

                foreach (XmlNode usrthreds in node["userThreads"])
                    userThreads.Add(new UserThread(int.Parse(usrthreds["threadId"].InnerText), DateTime.Parse(usrthreds["lastSeen"].InnerText)));

                user.userThreads = userThreads;

                parsedUsers.Add(new User(user.username, user.password, user.id, user.userThreads, user.deleted));
            }

            return new UserDatabase(parsedUsers);
        }

        public static UserDatabase TryParse(string filename)
        {
            List<User> parsed = new List<User>();
            parsed.Add(new User());
            try
            {
                XmlDocument xml = new XmlDocument();
                xml.Load(filename);         
                
                foreach (XmlNode node in xml.SelectSingleNode("//users").ChildNodes)
                {
                    try
                    {
                        UserStruct user = new UserStruct();

                        user.id = int.Parse(node["id"].InnerText);

                        if (!parsed.Any(n => n.id == user.id))
                        {
                            user.username = node["username"].InnerText;
                            user.password = node["password"].InnerText;
                            user.deleted = bool.Parse(node["deleted"].InnerText);

                            List<UserThread> userThreads = new List<UserThread>();

                            foreach (XmlNode usrthreds in node["userThreads"])
                                userThreads.Add(new UserThread(int.Parse(usrthreds["threadId"].InnerText), DateTime.Parse(usrthreds["lastSeen"].InnerText)));

                            user.userThreads = userThreads;

                            parsed.Add(new User(user.username, user.password, user.id, user.userThreads, user.deleted));
                        }                        
                    }
                    catch
                    {

                    }
                }
            }
            catch
            {

            }
            return new UserDatabase(parsed);
        }
    }
}
