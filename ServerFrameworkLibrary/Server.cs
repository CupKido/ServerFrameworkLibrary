using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ServerFramework
{
    public class Server
    {
        public Server()
        {
            posts = new List<request>();
            gets = new List<request>();
            port = 80;
        }

        ~Server()
        {
            CloseConnection();
        }
        bool isServerAlive = false;
        private int port;
        private List<request> posts;
        private List<request> gets;
        private Action whenListening;
        private Socket listener;

        static bool is404path = false;
        static private string page404 = "404";
        public void post(string action, Action<string, Socket> func)
        {

            posts.Add(new request(action, func));
            if (action == "/")
            {
                posts.Add(new request(action + "?", func));
            }
        }

        public void get(string action, Action<string, Socket> func)
        {
            gets.Add(new request(action, func));
            if (action == "/")
            {
                gets.Add(new request(action + "?", func));
            }
        }

        public void listen(int _port, Action foo)
        {
            port = _port;
            whenListening = foo;
        }

        public void SetPage404Path(string path)
        {
            is404path = true;
            page404 = path;
        }

        public void SetPage404Text(string Text)
        {
            is404path = false;
            page404 = Text;
        }

        public void StartServer()
        {
            if(isServerAlive) { return; }
            bool showtxt = false;
            Console.WriteLine("Show text recieved? (Y/N)");
            string input = Console.ReadLine();
            if (input.ToLower() == "y") showtxt = true;
            // Get Host IP Address that is used to establish a connection
            // In this case, we get one IP address of localhost that is IP : 127.0.0.1
            // If a host has multiple addresses, you will get a list of addresses
            IPHostEntry host = Dns.GetHostEntry("localhost");
            IPAddress ipAddress = host.AddressList[0];
            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, port);

            try
            {

                // Create a Socket that will use Tcp protocol
                listener = new Socket(IPAddress.Any.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                // A Socket must be associated with an endpoint using the Bind method
                listener.Bind(localEndPoint);
                // Specify how many requests a Socket can listen before it gives Server busy response.
                // We will listen 10 requests at a time
                listener.Listen(10);

                whenListening();
                Console.WriteLine("Waiting for a connection...");
                while (true)
                {
                    isServerAlive = true;
                    Socket handler = listener.Accept();
                    Console.WriteLine("Connection began with " + IPAddress.Parse(((IPEndPoint)handler.LocalEndPoint).Address.ToString()));

                    // Incoming data from the client.
                    string data = null;
                    byte[] bytes = null;

                    //while (true)
                    //{
                    bytes = new byte[1024];
                    int bytesRec = handler.Receive(bytes);
                    try
                    {
                    data += Encoding.ASCII.GetString(bytes, 0, bytesRec);
                    if (data.IndexOf("<EOF>") > -1)
                    {
                        //break;
                    }
                    }
                    catch
                    {
                        Console.WriteLine("ERROR");
                        data = "";
                    }

                    //}


                    if (showtxt) Console.WriteLine("Text received : {0}", data);
                    else
                    {
                        string[] p = data.Split(' ');
                        if (p.Length < 1)
                        {
                            Console.WriteLine("null sent");
                            send404(handler);
                        }
                        else
                        {
                        Console.WriteLine("Text received : {0}", p[1]);
                        }
                    }

                    takeActions(data, handler);
                    handler.Shutdown(SocketShutdown.Both);
                    handler.Close();
                }
                //byte[] msg = Encoding.ASCII.GetBytes(data);
                //handler.Send(msg);
                isServerAlive = false;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                CloseConnection();
            }

            if (showtxt)
            {
                Console.WriteLine("\n Press any key to continue...");
                Console.ReadKey();
            }
        }

        public void CloseConnection()
        {
            try
            {
                listener.Shutdown(SocketShutdown.Both);

            }
            catch
            {

            }
            try
            {
                listener.Close();

            }
            catch
            {

            }
            isServerAlive = false;
        }

        void takeActions(string data, Socket handler)
        {
            bool actionTaken = false;
            string[] p = data.Split(' ');
            if(p.Length < 1)
            {
                Console.WriteLine("null sent");
                send404(handler);
                return;
            }
            if (p[0] == "GET")
            {
                foreach (request r in gets)
                {
                    if (r.GetAction() == p[1] || r.GetAction() == "/All")
                    {
                        r.GetFoo()(data, handler);
                        actionTaken = true;
                    }
                }
            }
            else if (p[0] == "POST")
            {
                foreach (request r in posts)
                {
                    if (r.GetAction() == p[1] || r.GetAction() == "/All")
                    {
                        r.GetFoo()(data, handler);
                        actionTaken = true;
                    }
                }
            }
            if (actionTaken == false)
            {
                send404(handler);
            }
        }
        static public void send404(Socket handler)
        {
            if (is404path)
            {
                SendHTMLfile(handler, page404);
            }
            else
            {
                SendText(handler, page404);
            }
        }
        static public void SendHTMLfile(Socket handler, string path)
        {
            byte[] htmlfile = File.ReadAllBytes(path);
            handler.Send(htmlfile);
        }

        static public void SendText(Socket handler, string Text)
        {
            byte[] BText = Encoding.ASCII.GetBytes(Text);
            handler.Send(BText);
        }

        static public string getViewsFile(string name)
        {
            string exeDir = AppDomain.CurrentDomain.BaseDirectory;
            string res = exeDir.Substring(0, exeDir.Length - 17) + @"views\\" + name;
            return res;
        }

        static public Dictionary<string, string> GetPostParams(string data)
        {
            Dictionary<string, string> res = new Dictionary<string, string>();
            string[] allparams = data.Split("\r\n\r\n");
            if (allparams.Length != 2) return res;
            foreach (string param in allparams[1].Split('&'))
            {
                string[] theparam = param.Split('=');
                string thevalue = "";
                for (int i = 1; i < theparam.Length; i++)
                {
                    thevalue += theparam[i];
                }
                res.Add(theparam[0], thevalue);
            }
            return res;
        }
    }
}
