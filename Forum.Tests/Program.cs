using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Http;
using Http.HttpMessage;
using System.Net.Sockets;
using Forum2;
using HtmlAgilityPack;
using Forum2.Requests;
using Forum2.Items;
using Forum2.Items.ThreadItems;
using hah = System.Web.HttpUtility;
using Dns = System.Net.Dns;

namespace Forum.Tests
{
    class Program
    {
        static Server server;
        static Forum2.Forum forum;
        
        static void Main(string[] args)
        {
            forum = new Forum2.Forum();            
            forum.ThreadManager.AddThread(new Forum2.Items.User(), new Forum2.Items.ThreadItems.Header("New forum, new threads", "First thread on forum redesign"));
            forum.ThreadManager.AddThread(new Forum2.Items.User(), new Forum2.Items.ThreadItems.Header("Testing thread enumerability", "yes test"));
            forum.ContentManager.AddContent("icon.png", "", Content.ContentTypes.IMGPNG);
            var thread = forum.ThreadManager.GetThread(1);
            thread.AddComment(new Forum2.Items.ThreadItems.Comment("Yes", new Forum2.Items.User(), DateTime.Now, 1));
            while (true)
            {
                try
                {
                    Console.Write("Hostname: ");
                    server = new Server(new System.Net.IPEndPoint(Dns.GetHostAddresses(Console.ReadLine())[0], 80));
                    server.Start();
                    server.RequestRecieved += RequestRecieved;
                    break;
                }
                catch (Exception e)
                {
                    Console.WriteLine($"{e.Message}\r\n");
                }
            }
            Console.WriteLine("Server started");
            while (server.Connected) { Task.Delay(100).Wait(); }
            Console.WriteLine("Server stopped\r\nPress any key to continue...");
            Console.ReadKey();
        }

        static void SendNotFound(TcpClient client)
        {
            HttpResponse httpResponse = new HttpResponse();
            httpResponse.ResponseCode = Http.HttpMessage.Message.ResponseHeader.ResponseCodes.NOTFOUND;
            client.Client.Send(httpResponse.Serialize());
            Console.WriteLine($"{(int)httpResponse.ResponseCode} {httpResponse.ResponseCode}");
        }

        static int RequestRecieved(HttpRequest httpRequest, TcpClient client)
        {
            var uri = httpRequest.RequestURI.TrimStart('/');
            Console.WriteLine($"{httpRequest.Method} {httpRequest.RequestURI}");
            var user = GetUser(httpRequest);
            if (uri == "favicon.ico") { SendNotFound(client); } 
            else if (httpRequest.RequestURI == "/" || httpRequest.RequestURI == "")
            {
                SendMainPage(user, client);
            }
            else if (uri.StartsWith("t"))
            {
                var req = ProcessThreadRequest(httpRequest, client);
                if (req.Type == ThreadRequest.RequestTypes.Invalid) { /*SendMainPage(req.User, client);*/ SendRedirect("/", client); }
                else if (req.Type == ThreadRequest.RequestTypes.AddThread) {
                    forum.ThreadManager.AddThread(req.User, req.Header); /*SendMainPage(req.User, client);*/ SendRedirect("/", client);
                }
                else if (req.Type == ThreadRequest.RequestTypes.View) { if (req.User.username != "anonymous") req.User.Update(req.RequestThread.id); if (req.RequestThread.IsDeleted) SendRedirect("/", client); else SendThreadPage(req.RequestThread, client, req.User); }
                else if (req.Type == ThreadRequest.RequestTypes.DeleteThread) { if (req.User.username == "MrDoritos" || req.User == req.RequestThread.author) { forum.ThreadManager.Delete(req.RequestThread.id); SendRedirect("/", client); } }
                else if (req.Type == ThreadRequest.RequestTypes.AddComment) { forum.ThreadManager.AddComment(req.RequestThread, req.User, req.Text); SendRedirect($"/t{req.RequestThread.id}", client); }
                
            }            
            else if (uri == "login")
            {
                if (httpRequest.Method == Http.HttpMessage.Message.RequestHeader.Methods.POST)
                {
                    var log = ProcessLoginRequest(httpRequest, client);
                    if (log.invalid == true)
                    {
                        SendLoginPage(LoginFailType.UsernameExists, client);
                    }
                    else
                    {
                        if (forum.UserManager.Exists(log.username))
                        {
                            if (forum.UserManager.Exists(log.username, log.password))
                            {
                                SendRedirect("/", client, new KeyValuePair<string, string>("token", forum.UserManager.RenewToken(forum.UserManager.GetUser(log.username, log.password))));
                            }
                            else
                            {
                                SendLoginPage(LoginFailType.IncorrectCreds, client);
                            }
                        }
                        else
                        {
                            var newuser = forum.UserManager.AddUser(log.username, log.password);
                            SendRedirect("/", client, new KeyValuePair<string, string>("token", newuser.currentToken));
                        }
                    }
                }
                else
                {
                    SendLoginPage(LoginFailType.None, client);
                }
            }
            else if (uri.StartsWith("content"))
            {
                SendContent(httpRequest, client);
            }
            else
            {
                SendRedirect("/", client);
            }
            return 0;
        }

        static LoginRequest ProcessLoginRequest(HttpRequest httpRequest, TcpClient client)
        {
            if (httpRequest.form != null)
            {
                if (httpRequest.form.UrlEncode.messages.All(n => (n.name.ToLower() == "username" || n.name.ToLower() == "password") && (n.value != null && n.value.Length > 0)))
                {
                    return new LoginRequest() { password = httpRequest.form.UrlEncode.messages.First(n => n.name.ToLower() == "password").value, username = httpRequest.form.UrlEncode.messages.First(n => n.name.ToLower() == "username").value };
                }
                else
                {
                    return new LoginRequest() { invalid = true };
                }
            }
            else
            {
                return new LoginRequest() { invalid = true };
            }
        }

        static ThreadRequest ProcessThreadRequest(HttpRequest httpRequest, TcpClient client)
        {
            string[] reqs = httpRequest.RequestURI.TrimStart('/').Split('&');
            var user = GetUser(httpRequest);
            if (!int.TryParse(reqs[0].Remove(0, 1), out int threadid)) {
                return new ThreadRequest(ThreadRequest.RequestTypes.Invalid, user); }
            var thread = forum.ThreadManager.GetThread(threadid);
            if (reqs.Length < 2)
            {
                if (thread == null) {
                    return new ThreadRequest(ThreadRequest.RequestTypes.Invalid, user); }
                return new ThreadRequest(ThreadRequest.RequestTypes.View, thread, user);
            }
            ThreadRequest.RequestTypes requestTypes = ThreadRequest.RequestTypes.Invalid;
            foreach (var a in reqs)
                switch (a[0])
                {
                    case 'a':
                        if (httpRequest.form != null)
                        {
                            if (httpRequest.form.UrlEncode.messages.All(n => (n.name == "title" || n.name == "content") && (n.value != null && n.value.Length > 0)))
                            {
                                return new ThreadRequest(ThreadRequest.RequestTypes.AddThread, new Header(httpRequest.form.UrlEncode.messages.FirstOrDefault(n => n.name == "content").value, httpRequest.form.UrlEncode.messages.FirstOrDefault(n => n.name == "title").value), user);
                            }
                            else
                            {
                                return new ThreadRequest(ThreadRequest.RequestTypes.Invalid, user);
                            }
                        }
                        return new ThreadRequest(ThreadRequest.RequestTypes.Invalid, user);
                    case 'c':
                        requestTypes = ThreadRequest.RequestTypes.AddComment;
                        if (httpRequest.form != null)
                        {
                            if (httpRequest.form.UrlEncode.messages.Any(n => n.name == "content") && thread != null)
                            {
                                return new ThreadRequest(ThreadRequest.RequestTypes.AddComment, thread, httpRequest.form.UrlEncode.messages.FirstOrDefault(n => n.name == "content").value, user);
                            }
                            else
                            {
                                return new ThreadRequest(ThreadRequest.RequestTypes.Invalid, user);
                            }
                        }
                        return new ThreadRequest(ThreadRequest.RequestTypes.Invalid, user);
                    case 'd':
                        requestTypes = ThreadRequest.RequestTypes.DeleteComment;
                        break;
                    case 'e':
                        requestTypes = ThreadRequest.RequestTypes.DeleteThread;
                        break;
                }
            return new ThreadRequest(requestTypes, thread, user);
        }

        static void SendContent(HttpRequest httpRequest, TcpClient client)
        {
            var content = GetContent(httpRequest.RequestURI);
            HttpResponse httpResponse;
            if (content == null)
            {
                httpResponse = new HttpResponse() { ResponseCode = Http.HttpMessage.Message.ResponseHeader.ResponseCodes.NOTFOUND };
            }
            else
            {
                httpResponse = new HttpResponse(new Http.HttpMessage.Message.ResponseHeader(new Http.HttpMessage.Message.Header(), GetEqString(content.ContentType)), new Http.HttpMessage.Message.Content(forum.ContentManager.GetFile(content)));
            }
            AddNoCache(httpResponse);
            client.Client.Send(httpResponse.Serialize());
            Console.WriteLine($"{(int)httpResponse.ResponseCode} {httpResponse.ResponseCode}");
            //client.Client.Send(HttpResponse.Serialize(httpResponse.content, httpResponse as Http.HttpMessage.Message.ResponseHeader));
        }

        static void AddNoCache(HttpResponse response)
        {
            var old = response.headerParameters.ToList();
            old.Add(new Http.HttpMessage.Message.HeaderParameter(new Http.HttpMessage.Message.HeaderVariable[1] { new Http.HttpMessage.Message.HeaderVariable("cache-control", "no-cache") }));
            response.headerParameters = old.ToArray();
        }

        static void AddParam(string name, string value, HttpResponse response)
        {
            var old = response.headerParameters.ToList();
            old.Add(new Http.HttpMessage.Message.HeaderParameter(new Http.HttpMessage.Message.HeaderVariable[1] { new Http.HttpMessage.Message.HeaderVariable(name, value) }));
            response.headerParameters = old.ToArray();
        }

        static HttpResponse.ContentTypes GetEqString(Content.ContentTypes content)
        {
            switch (content)
            {
                case Content.ContentTypes.IMGJPEG:
                    return Http.HttpMessage.Message.ResponseHeader.ContentTypes.IMAGEJPEG;
                case Content.ContentTypes.IMGPNG:
                    return Http.HttpMessage.Message.ResponseHeader.ContentTypes.IMAGEPNG;
                default:
                    return Http.HttpMessage.Message.ResponseHeader.ContentTypes.PLAIN;
            }
        }

        static Content GetContent(string uri)
        {
            string cut = uri.Split('&').LastOrDefault() ?? "";
            if (int.TryParse(cut, out int id))
            {
                if (forum.ContentManager.Exists(id) && forum.ContentManager.FileExists(id))
                {
                    return forum.ContentManager.GetContent(id);
                }
                return null;
            }
            else
            {
                return null;
            }
        }

        static User GetUser(HttpRequest httpRequest)
        {
            var variables = httpRequest.headerParameters.Select(n => n.HeaderVariables);
            if (variables.Any(n => n.Any(m => m.name.ToLower() == "cookie")))
            {
                var token = variables.First(n => n.Any(m => m.name.ToLower() == "cookie")).First();
                var strtoken = token.value.Split(';');
                string tokendds;
                if ((tokendds = strtoken.FirstOrDefault(n => n.Split('=')[0] == "token").Remove(0, 6)) != null)
                {
                    return forum.UserManager.TryAuth(tokendds);
                }
                else
                {
                    return User.Anonymous;
                }
            } else { return User.Anonymous; }
        }

        static void SendRedirect(string uri, TcpClient client)
        {
            HttpResponse httpResponse = new HttpResponse();
            httpResponse.headerParameters = new Http.HttpMessage.Message.HeaderParameter[1] { new Http.HttpMessage.Message.HeaderParameter(new Http.HttpMessage.Message.HeaderVariable[1] { new Http.HttpMessage.Message.HeaderVariable("location", uri) } ) };
            httpResponse.ResponseCode = Http.HttpMessage.Message.ResponseHeader.ResponseCodes.PERMREDIRECT;
            AddNoCache(httpResponse);
            client.Client.Send(httpResponse.Serialize());
            Console.WriteLine($"{(int)httpResponse.ResponseCode} {httpResponse.ResponseCode}");
        }

        static void SendRedirect(string uri, TcpClient client, KeyValuePair<string, string> cookie)
        {
            HttpResponse httpResponse = new HttpResponse();
            httpResponse.headerParameters = new Http.HttpMessage.Message.HeaderParameter[2] { new Http.HttpMessage.Message.HeaderParameter(new Http.HttpMessage.Message.HeaderVariable[1] { new Http.HttpMessage.Message.HeaderVariable("location", uri) }), new Http.HttpMessage.Message.HeaderParameter(new Http.HttpMessage.Message.HeaderVariable[1] { new Http.HttpMessage.Message.HeaderVariable("set-cookie", $"{cookie.Key}={cookie.Value}")}) };
            httpResponse.ResponseCode = Http.HttpMessage.Message.ResponseHeader.ResponseCodes.PERMREDIRECT;
            AddNoCache(httpResponse);
            client.Client.Send(httpResponse.Serialize());
            Console.WriteLine($"{(int)httpResponse.ResponseCode} {httpResponse.ResponseCode}");
        }

        public enum LoginFailType
        {
            None = 0,
            UsernameExists = 1,
            IncorrectCreds = 2,
        }

        static void SendLoginPage(LoginFailType loginFailType, TcpClient client, string presetusername = "", string presetpassword = "")
        {
            HttpResponse httpResponse = new HttpResponse();
            httpResponse.content = new Http.HttpMessage.Message.Content(Serialize(GetLoginPage(loginFailType, presetusername, presetpassword)));
            AddNoCache(httpResponse);

            client.Client.Send(httpResponse.Serialize());
            Console.WriteLine($"{(int)httpResponse.ResponseCode} {httpResponse.ResponseCode}");
        }

        static void SendMainPage(Forum2.Items.User user, TcpClient client)
        {
            HttpResponse httpResponse = new HttpResponse();
            httpResponse.content = new Http.HttpMessage.Message.Content(Serialize(GetMainPage(user, user.username == "MrDoritos")));
            AddNoCache(httpResponse);

            client.Client.Send(httpResponse.Serialize());
            Console.WriteLine($"{(int)httpResponse.ResponseCode} {httpResponse.ResponseCode}");
        }

        static void SendThreadPage(Thread thread, TcpClient client, User user)
        {
            HttpResponse httpResponse = new HttpResponse();
            httpResponse.content = new Http.HttpMessage.Message.Content(Serialize(GetThreadPage(thread, user)));
            AddNoCache(httpResponse);

            client.Client.Send(httpResponse.Serialize());
            Console.WriteLine($"{(int)httpResponse.ResponseCode} {httpResponse.ResponseCode}");
        }

        static byte[] Serialize(HtmlDocument doc)
        {
            System.IO.MemoryStream memoryStream = new System.IO.MemoryStream();
            doc.Save(memoryStream);
            return memoryStream.ToArray();
        }

        static HtmlDocument GetLoginPage(LoginFailType loginFailType, string presetusername = "", string presetpassword = "")
        {
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml("<html><head /><body /></html>");
            var body = doc.DocumentNode.Descendants().First(n => n.Name == "body");
            var div = HtmlNode.CreateNode("<div />");
            var form = HtmlNode.CreateNode("<form method=\"POST\" action=\"/login\"/>");
            form.AppendChild(HtmlNode.CreateNode("<p style=\"margin: 1px;\">Username</p>"));
            //form.AppendChild(HtmlNode.CreateNode("<br>"));
            form.AppendChild(HtmlNode.CreateNode("<input type=\"text\" name=\"username\">"));
            //form.AppendChild(HtmlNode.CreateNode("<br>"));
            form.AppendChild(HtmlNode.CreateNode("<p style=\"margin: 1px;\">Password</p>"));
            //form.AppendChild(HtmlNode.CreateNode("<br>"));
            form.AppendChild(HtmlNode.CreateNode("<input type=\"password\" name=\"password\">"));
            form.AppendChild(HtmlNode.CreateNode("<br>"));
            form.AppendChild(HtmlNode.CreateNode("<input type=\"submit\" style=\"margin: 3px;\" value=\"Log in or register\">"));
            form.AppendChild(HtmlNode.CreateNode("<output for=\"username password\">"));
            div.AppendChild(form);

            switch (loginFailType)
            {
                case LoginFailType.None:
                    break;
                case LoginFailType.IncorrectCreds:
                    div.AppendChild(HtmlTextNode.CreateNode("<strong>Incorrect username or password!</strong>"));
                    break;
                case LoginFailType.UsernameExists:
                    div.AppendChild(HtmlTextNode.CreateNode("<strong>Invalid input!</strong>"));
                    break;
            }
            body.AppendChild(div);
            return doc;
        }

        static HtmlNode styles = HtmlNode.CreateNode("<style>" +
            "#thread:hover { background-color: white; } " +
            "#thread { background-color:rgba(255, 255, 255, 0.9); } " +
            "</style>");

        static HtmlDocument GetMainPage(Forum2.Items.User curUser, bool admin = false)
        {
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml("<html style=\"font-family: arial;\"><head /><body style=\"background-image: url('/content&1')\" /></html>");
            
            var body = doc.DocumentNode.Descendants().First(n => n.Name == "body");
            HtmlNode currentUser = HtmlNode.CreateNode("<p style=\"text-align: right; \" />");
            if (curUser == User.Anonymous)
            {
                currentUser.AppendChild(HtmlNode.CreateNode("<a href=\"/login\">Login</a> "));
                currentUser.AppendChild(HtmlNode.CreateNode($"<strong> anonymous</strong>"));
            }
            else
            {
                currentUser.AppendChild(HtmlTextNode.CreateNode($"<strong>{curUser.username}</strong>"));
            }
            body.AppendChild(styles);
            body.AppendChild(currentUser);
            body.AppendChild(GetAddThread());
            body.AppendChild(GetThreads(curUser, admin));
            return doc;
        }

        static HtmlDocument GetThreadPage(Thread thread, User curUser)
        {
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml("<html style=\"font-family: arial;\"><head /><body /></html>");
            var body = doc.DocumentNode.Descendants().First(n => n.Name == "body");
            HtmlNode currentUser = HtmlNode.CreateNode("<p style=\"text-align: right; margin: 5px; \" />");
            if (curUser == User.Anonymous)
            {
                currentUser.AppendChild(HtmlNode.CreateNode("<a href=\"/login\">Login</a> "));
                currentUser.AppendChild(HtmlNode.CreateNode($"<strong> anonymous</strong>"));
            }
            else
            {
                currentUser.AppendChild(HtmlTextNode.CreateNode($"<strong>{curUser.username}</strong>"));
            }
            body.AppendChild(currentUser);
            body.AppendChild(GetHeader(thread));
            body.AppendChild(GetMessages(thread));
            body.AppendChild(GetAddComment(thread));
            return doc;
        }

        static HtmlNode GetAddComment(Thread thread)
        {
            HtmlNode div = HtmlNode.CreateNode("<div />");
            HtmlNode form = HtmlNode.CreateNode($"<form method=\"POST\" action=\"t{thread.id}&c\" />");

            form.InnerHtml = "<textarea type=\"text\" name=\"content\" rows=\"5\" style=\"width: 100%;\" /><output for=\"content\" />";
            HtmlNode submit = HtmlNode.CreateNode("<input type=\"submit\" value=\"Add Comment\" style=\"margin: 1%; width: 48%;\">");
            form.AppendChild(submit);

            div.AppendChild(form);
            return div;
        }

        static HtmlNode GetAddThread()
        {
            HtmlNode div = HtmlNode.CreateNode("<div style=\"width: 60%; margin-right: 20%;\" />");
            HtmlNode form = HtmlNode.CreateNode("<form method=\"POST\" action=\"t0&a\" />");
            HtmlNode br = HtmlNode.CreateNode("<br>");

            form.AppendChild(HtmlTextNode.CreateNode("<p style=\"margin: 3px;\">Title</p>"));
            HtmlNode title = HtmlNode.CreateNode("<input type=\"text\" style=\"width: 100%;\" name=\"title\">");
            form.AppendChild(title);

            form.AppendChild(HtmlTextNode.CreateNode("<p style=\"margin: 3px; \" >Text</p>"));
            HtmlNode content = HtmlNode.CreateNode("<textarea type=\"text\" name=\"content\" rows=\"5\" style=\"width: 100%;\">");
            form.AppendChild(content);

            form.AppendChild(br);
            HtmlNode submit = HtmlNode.CreateNode("<input type=\"submit\" value=\"Create Thread\" style=\"margin: 1%; width: 48%;\">");
            form.AppendChild(submit);

            HtmlNode output = HtmlNode.CreateNode("<output for=\"title content\">");

            div.AppendChild(form);
            return div;
        }

        static string GetColor(Thread thread, User user)
        {
            if (user.id == 0) { return "solid black"; }
            switch (user.Seen(thread))
            {
                case UserThread.State.New:
                    return "dashed green";
                case UserThread.State.Read:
                    return "solid black";
                case UserThread.State.Unread:
                    return "dashed blue";
                default: return "dashed red";
            }
        }

        static HtmlNode GetThreads(User user, bool admin = false)
        {
            HtmlNode threads = HtmlNode.CreateNode("<div />");
            foreach (var thread in forum.ThreadManager.GetThreads().OrderBy(n => n.creationTime.Ticks).Where(n => !n.IsDeleted))
            {
                //HtmlNode link = HtmlNode.CreateNode("<a />");
                //link.Attributes.Add("href", $"/t{thread.id}");

                HtmlNode threadnode = HtmlNode.CreateNode("<div />");
                threadnode.Attributes.Add("style", $"border: 5px {GetColor(thread, user)}; margin: 5px; cursor: pointer; ");
                threadnode.Attributes.Add("onClick", $"window.location='/t{thread.id}'");
                threadnode.Attributes.Add("id", $"thread");

                HtmlNode authordate = HtmlNode.CreateNode("<div style=\"display: block;\" />");

                HtmlNode authordiv = HtmlNode.CreateNode("<div style=\"display: inline-block; width: 50%;\" />");
                HtmlNode authorp = HtmlNode.CreateNode("<p style=\"margin: 5px; \"/>");
                HtmlNode author = HtmlNode.CreateNode("<strong />");
                author.AppendChild(HtmlTextNode.CreateNode(hah.HtmlEncode(thread.author.username)));
                authorp.AppendChild(author);
                authordiv.AppendChild(authorp);

                HtmlNode datediv = HtmlNode.CreateNode("<div style=\"display: inline-block; width: 50%; text-align:right;\" />");
                HtmlNode date = HtmlNode.CreateNode("<p style=\"margin: 5px; \" />");
                date.AppendChild(HtmlTextNode.CreateNode(thread.creationTime.ToString("MM/dd/yy HH:mm")));
                datediv.AppendChild(date);

                authordate.AppendChild(authordiv);
                authordate.AppendChild(datediv);

                threadnode.AppendChild(authordate);

                HtmlNode title = HtmlNode.CreateNode("<h2 style=\"margin: 1%; margin-left: 3%; margin-bottom: 2%;\" />");
                title.AppendChild(HtmlTextNode.CreateNode(hah.HtmlEncode(thread.title)));
                threadnode.AppendChild(title);

                if (admin)
                {
                    HtmlNode delete = HtmlNode.CreateNode($"<a href=\"/t{thread.id}&e\">Delete</a>");
                    threadnode.AppendChild(delete);
                }
                //link.AppendChild(threadnode);
                //threads.AppendChild(link);

                threads.AppendChild(threadnode);
            }
            return threads;
        }

        static HtmlNode GetHeader(Thread thread)
        {
            HtmlNode header = HtmlNode.CreateNode("<div />");
            header.Attributes.Add("style", "border: 5px solid black; margin: 5px; padding: 5px;");

            //HtmlNode authorp = HtmlNode.CreateNode("<p />");
            //HtmlNode author = HtmlNode.CreateNode("<strong />");
            HtmlNode title = HtmlNode.CreateNode("<h2 style=\"margin-top: 1%; margin-left: 3%; margin-right: 3%; margin-bottom: 2%;\" />");
            HtmlNode text = HtmlNode.CreateNode("<p style=\"margin-top: 1%; margin-left: 3%; margin-right: 3%; margin-bottom: 2%;\" />");
            //HtmlNode date = HtmlNode.CreateNode("<p />");

            //author.AppendChild(HtmlTextNode.CreateNode(hah.HtmlEncode(thread.author.username)));
            title.AppendChild(HtmlTextNode.CreateNode(hah.HtmlEncode(thread.title.Trim('\r', '\n'))));
            text.InnerHtml += (hah.HtmlEncode(thread.headertext).Trim('\r', '\n').Replace("\n","<br />").Replace("\r", ""));
            
            //date.AppendChild(HtmlTextNode.CreateNode(thread.creationTime.ToString("MM/dd/yy HH:mm")));

            //Imported code

            HtmlNode authordate = HtmlNode.CreateNode("<div style=\"display: block;\" />");

            HtmlNode authordiv = HtmlNode.CreateNode("<div style=\"display: inline-block; width: 50%;\" />");
            HtmlNode authorp = HtmlNode.CreateNode("<p style=\"margin: 5px; \"/>");
            HtmlNode author = HtmlNode.CreateNode("<strong />");
            author.AppendChild(HtmlTextNode.CreateNode(hah.HtmlEncode(thread.author.username)));
            authorp.AppendChild(author);
            authordiv.AppendChild(authorp);

            HtmlNode datediv = HtmlNode.CreateNode("<div style=\"display: inline-block; width: 50%; text-align:right;\" />");
            HtmlNode date = HtmlNode.CreateNode("<p style=\"margin: 5px; \" />");
            date.AppendChild(HtmlTextNode.CreateNode(thread.creationTime.ToString("MM/dd/yy HH:mm")));
            datediv.AppendChild(date);

            authordate.AppendChild(authordiv);
            authordate.AppendChild(datediv);

            header.AppendChild(authordate);

            //\\/


            //authorp.AppendChild(author);
            //header.AppendChild(authorp);
            header.AppendChild(title);
            header.AppendChild(text);
            //header.AppendChild(date);

            return header;
        }

        static HtmlNode GetMessages(Thread thread)
        {
            HtmlNode messages = HtmlNode.CreateNode("<div />");
            foreach (var message in thread.GetUndeletedComments())
            {
                HtmlNode comment = HtmlNode.CreateNode("<div />");
                comment.Attributes.Add("style", "border: 5px solid black; margin: 5px; padding: 5px;");

                //HtmlNode author = HtmlNode.CreateNode("<p />");
                HtmlNode content = HtmlNode.CreateNode("<p style=\"margin-top: 1%; margin-left: 3%; margin-right: 3%; margin-bottom: 2%;\" />");
                //HtmlNode date = HtmlNode.CreateNode("<p />");

                //author.AppendChild(HtmlTextNode.CreateNode(hah.HtmlEncode(message.Author.username)));
                content.InnerHtml += (hah.HtmlEncode(message.Text).Trim('\r', '\n').Replace("\n", "<br />").Replace("\r", ""));
                //date.AppendChild(HtmlTextNode.CreateNode(thread.creationTime.ToString("MM/dd/yy HH:mm")));

                // Commend import

                HtmlNode authordate = HtmlNode.CreateNode("<div style=\"display: block;\" />");

                HtmlNode authordiv = HtmlNode.CreateNode("<div style=\"display: inline-block; width: 50%;\" />");
                HtmlNode authorp = HtmlNode.CreateNode("<p style=\"margin: 5px; \"/>");
                HtmlNode author = HtmlNode.CreateNode("<strong />");
                author.AppendChild(HtmlTextNode.CreateNode(hah.HtmlEncode(thread.author.username)));
                authorp.AppendChild(author);
                authordiv.AppendChild(authorp);

                HtmlNode datediv = HtmlNode.CreateNode("<div style=\"display: inline-block; width: 50%; text-align:right;\" />");
                HtmlNode date = HtmlNode.CreateNode("<p style=\"margin: 5px; \" />");
                date.AppendChild(HtmlTextNode.CreateNode(thread.creationTime.ToString("MM/dd/yy HH:mm")));
                datediv.AppendChild(date);

                authordate.AppendChild(authordiv);
                authordate.AppendChild(datediv);

                comment.AppendChild(authordate);

                //\/\/\\

                //comment.AppendChild(author);
                comment.AppendChild(content);
                //comment.AppendChild(date);

                messages.AppendChild(comment);
            }
            return messages;
        }
    }
}
