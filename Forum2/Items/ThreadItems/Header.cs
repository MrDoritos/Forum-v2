using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Forum2.Items.ThreadItems
{
    public class Header
    {
        protected Header() { headertext = ""; title = ""; }
        private Header(String content) { this.headertext = content ?? ""; title = ""; }
        public Header(String content, String title) { this.headertext = content ?? ""; this.title = title ?? ""; }
        public Header(Header header) { if (header != null) { this.headertext = header.headertext ?? ""; this.title = header.title ?? ""; } else { headertext = ""; title = ""; } }

        public string headertext;
        public string title;
    }
}
