using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Forum2.Items
{
    public class Content
    {
        private string relativePath;
        public string FileName { get; private set; }
        public int id;
        public ContentTypes ContentType { get; private set; }
        public String RelativeFilePath
        {
            get { return RelativePath + FileName; }
        }
        public String RelativePath
        {
            get { return relativePath; }
            set { relativePath = GetPath(value); }
        }
        private static string GetPath(string input)
        {
            if (input.Length < 1)
            {
                return "";
            }
            else
            {
                if (input.StartsWith("."))
                {
                    return input;
                }
                else
                {
                    return input.TrimEnd('\\') + "\\";
                }
            }
        }

        public bool Exists
        {
            get { return File.Exists(RelativeFilePath); }
        }
        private Content(String Filename, String RelativePath) { FileName = Filename; this.RelativePath = RelativePath; }
        public Content(ContentTypes type, String FileName, String RelativePath, Int32 Id) { this.FileName = FileName; this.RelativePath = RelativePath; id = Id; this.ContentType = type; }
        
        public enum ContentTypes
        {
            IMGPNG = 0,
            IMGJPEG = 1,
            PLAIN = 2,
        }
    }
}
