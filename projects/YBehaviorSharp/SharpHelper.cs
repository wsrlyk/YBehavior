using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Linq;

namespace YBehaviorSharp
{
    public delegate bool OnNodeLoaded(IntPtr pNode, IntPtr pData);
    public delegate NodeState OnNodeUpdate(IntPtr pNode, IntPtr pAgent);
    public delegate string LoadDataCallback(string treeName);

    public enum NodeState
    {
        NS_INVALID = -1,
        NS_SUCCESS,
        NS_FAILURE,
        NS_BREAK,
        NS_RUNNING,
    };

    public class SharpHelper
    {
        public static LoadDataCallback LoadDataCallback
        {
            get { return s_LoadDataCallback; }
            set { s_LoadDataCallback = value; }
        }
        private static LoadDataCallback s_LoadDataCallback = null;

        static List<SBehaviorNode> s_NodeList = new List<SBehaviorNode>();
        public static void Init()
        {
            if (LoadDataCallback != null)
                RegisterLoadData(LoadDataCallback);

            var subTypeQuery = from t in System.Reflection.Assembly.GetExecutingAssembly().GetTypes()
                               where SUtility.IsSubClassOf(t, typeof(SBehaviorNode))
                               select t;

            foreach (var type in subTypeQuery)
            {
                SBehaviorNode node = Activator.CreateInstance(type) as SBehaviorNode;
                node.Register();
                s_NodeList.Add(node);
            }
        }

        [DllImport("YBehavior.dll")]
        static public extern IntPtr CreateEntity();

        [DllImport("YBehavior.dll")]
        static public extern void DeleteEntity(IntPtr pEntity);

        [DllImport("YBehavior.dll")]
        static public extern IntPtr CreateAgent(IntPtr pEntity);

        [DllImport("YBehavior.dll")]
        static public extern void DeleteAgent(IntPtr pAgent);

        [DllImport("YBehavior.dll", CallingConvention = CallingConvention.StdCall)]
        static public extern void RegisterSharpNode(
            string name,
            OnNodeLoaded onload,
            OnNodeUpdate onupdate);

        [DllImport("YBehavior.dll", CallingConvention = CallingConvention.StdCall)]
        static public extern void RegisterLoadData(LoadDataCallback loaddata);

        [DllImport("YBehavior.dll")]
        static public extern void SetTree(IntPtr pAgent, string treename);

        [DllImport("YBehavior.dll")]
        static public extern void Tick(IntPtr pAgent);
    }
}
