using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using YBehaviorSharp;

namespace TestSharp
{
    class Program
    {
        static void Main(string[] args)
        {
            YBehaviorSharp.SharpHelper.LoadDataCallback = new YBehaviorSharp.LoadDataCallback(LoadData);
            YBehaviorSharp.SharpHelper.Init();
            YBehaviorSharp.SEntity entity = new YBehaviorSharp.SEntity();

            YBehaviorSharp.SAgent agent = new YBehaviorSharp.SAgent(entity);
            string[] state2tree = new string[] { "Main", "Test0"};
            YBehaviorSharp.SharpHelper.SetBehavior(agent.Core, "EmptyFSM", state2tree, 2, null, 0);

            int i = 0;
            while(++i < 1000)
            {
                YBehaviorSharp.SharpHelper.Tick(agent.Core);
                System.Threading.Thread.Sleep(1000);
            }

            YBehaviorSharp.SharpHelper.DeleteAgent(agent.Core);
            YBehaviorSharp.SharpHelper.DeleteEntity(entity.Core);
            Console.Read();
        }

        static string LoadData(string treename)
        {
            FileStream fileStream = new FileStream(treename, FileMode.Open, FileAccess.Read, FileShare.Read);
            byte[] bytes = new byte[fileStream.Length];
            fileStream.Read(bytes, 0, bytes.Length);
            fileStream.Close();

            string str = System.Text.Encoding.UTF8.GetString(bytes);
            return str;
        }
    }
}
