using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Forum2.Items;
using System.IO;

namespace Forum2.ContentManagement
{
    public class ContentManager
    {
        private List<Content> contents;

        public int NextId { get => GetNextId(); }
        
        public ContentManager() { contents = new List<Content>(); }

        public Content AddContent(string filename, string relativefilepath, Content.ContentTypes contentType)
        {
            var con = new Content(contentType, filename, relativefilepath, NextId);
            contents.Add(con);
            return con;
        }

        public Content GetContent(int id)
        {
            return contents.FirstOrDefault(n => n.id == id);
        }

        public bool Exists(int id)
        {
            return contents.Any(n => n.id == id);
        }

        public bool FileExists(int id)
        {
            return GetContent(id).Exists;
        }

        public byte[] GetFile(int id)
        {
            return File.ReadAllBytes(GetContent(id).RelativeFilePath);
        }

        public byte[] GetFile(Content file)
        {
            return File.ReadAllBytes(file.RelativeFilePath);
        }

        public int GetNextId()
        {
            return contents.Count + 1;
        }
    }
}
