using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace TestSharp
{
    public class SharpHelper
    {
        [DllImport("YBehavior.dll")]
        static public extern IntPtr CreateEntity();

        [DllImport("YBehavior.dll")]
        static public extern void DeleteEntity(IntPtr pEntity);

        [DllImport("YBehavior.dll")]
        static public extern IntPtr CreateAgent(IntPtr pEntity);

        [DllImport("YBehavior.dll")]
        static public extern void DeleteAgent(IntPtr pAgent);
    }

    class Program
    {
        static void Main(string[] args)
        {
            YBehaviorSharp.SharpHelper.LoadDataCallback = new YBehaviorSharp.LoadDataCallback(LoadData);
            YBehaviorSharp.SharpHelper.Init();
            YBehaviorSharp.SharpHelper.CreateEntity();
            YBehaviorSharp.SEntity entity = new YBehaviorSharp.SEntity();

            YBehaviorSharp.SAgent agent = new YBehaviorSharp.SAgent(entity);
            YBehaviorSharp.SharpHelper.SetTree(agent.Core, "Monster_BlackCrystal3");

            int i = 0;
            while(++i < 3)
            {
                YBehaviorSharp.SharpHelper.Tick(agent.Core);
            }
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
