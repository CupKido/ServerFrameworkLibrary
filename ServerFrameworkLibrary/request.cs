using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ServerFramework
{
    class request
    {
        public request(string _action, Action<string, Socket> _foo)
        {
            action = _action;
            foo = _foo;
        }
        Action<string, Socket> foo;
        string action;
        public string GetAction()
        {
            return action;
        }

        public Action<string, Socket> GetFoo()
        {
            return foo;
        }
    }
}
