using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediatorLib
{

    [Serializable]
    public static class ConversationState
    {
        public static List<string> Users { get; set; } = new List<string>();
        public static Dictionary<string, int> UserMsgs { get; set; } = new Dictionary<string, int>();

        public static void AddUser(string uname)
        {
            if (!Users.Contains(uname)) Users.Add(uname);
        }

        public static void RegisterMessage(string uname, string msg)
        {
            AddUser(uname);
            if (!UserMsgs.Keys.Contains(uname)) UserMsgs.Add(uname, 1);
            else UserMsgs[uname]++;
        }

    }

}
