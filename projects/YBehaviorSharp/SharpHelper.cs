using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Linq;

namespace YBehaviorSharp
{
    using TYPEID = System.Int32;
    using KEY = System.Int32;
    using INT = System.Int32;
    using BOOL = System.Int16;
    using FLOAT = System.Single;
    using ULONG = System.UInt64;
    using STRING = System.String;
    using Bool = System.Int16;

    public delegate bool OnNodeLoaded(IntPtr pNode, IntPtr pData);
    public delegate NodeState OnNodeUpdate(IntPtr pNode, IntPtr pAgent);
    public delegate string LoadDataCallback(string treeName);
    public delegate void LogCallback();

    public enum NodeState
    {
        NS_INVALID = -1,
        NS_SUCCESS,
        NS_FAILURE,
        NS_BREAK,
        NS_RUNNING,
    };
    public struct Vector3
	{
		public float x;
        public float y;
        public float z;

        public static Vector3 zero = new Vector3() { x = 0, y = 0, z = 0 };
    }

    public struct EntityWrapper
    {
    }

    public class GetClassType<T>
    {
        static TYPEID id = -1;
        static TYPEID vecid = -1;
        public static TYPEID ID { get { return id; } }
        public static TYPEID VecID { get { return vecid; } }

        static GetClassType()
        {
            GetClassType<BOOL>.id = SharpHelper.GetClassTypeNumberIdBool();
            GetClassType<bool>.id = SharpHelper.GetClassTypeNumberIdBool();
            GetClassType<INT>.id = SharpHelper.GetClassTypeNumberIdInt();
            GetClassType<FLOAT>.id = SharpHelper.GetClassTypeNumberIdFloat();
            GetClassType<ULONG>.id = SharpHelper.GetClassTypeNumberIdUlong();
            GetClassType<STRING>.id = SharpHelper.GetClassTypeNumberIdString();
            GetClassType<Vector3>.id = SharpHelper.GetClassTypeNumberIdVector3();
            GetClassType<SEntity>.id = SharpHelper.GetClassTypeNumberIdEntityWrapper();

            GetClassType<BOOL>.vecid = SharpHelper.GetClassTypeNumberIdVecBool();
            GetClassType<bool>.vecid = SharpHelper.GetClassTypeNumberIdVecBool();
            GetClassType<INT>.vecid = SharpHelper.GetClassTypeNumberIdVecInt();
            GetClassType<FLOAT>.vecid = SharpHelper.GetClassTypeNumberIdVecFloat();
            GetClassType<ULONG>.vecid = SharpHelper.GetClassTypeNumberIdVecUlong();
            GetClassType<STRING>.vecid = SharpHelper.GetClassTypeNumberIdVecString();
            GetClassType<Vector3>.vecid = SharpHelper.GetClassTypeNumberIdVecVector3();
            GetClassType<SEntity>.vecid = SharpHelper.GetClassTypeNumberIdVecEntityWrapper();
        }
    }

    public class VERSION
    {
#if (UNITY_IPHONE || UNITY_TVOS || UNITY_SWITCH || UNITY_WEBGL) && !UNITY_EDITOR
        public const string dll    = "YBehavior";
#elif (UNITY_PS4) && !UNITY_EDITOR
        public const string dll    = "YBehavior";
#elif (UNITY_PS4) && DEVELOPMENT_BUILD
        public const string dll    = "YBehavior";
#elif (UNITY_PSP2 || UNITY_WIIU) && !UNITY_EDITOR
        public const string dll    = "YBehavior";
/* Linux defines moved before the Windows define, otherwise Linux Editor tries to use Win lib when selected as build target.*/
#elif (UNITY_EDITOR_LINUX) || ((UNITY_STANDALONE_LINUX || UNITY_ANDROID || UNITY_XBOXONE) && DEVELOPMENT_BUILD)
        public const string dll    = "YBehavior";
#elif (UNITY_EDITOR_OSX || UNITY_EDITOR_WIN) || ((UNITY_STANDALONE_OSX || UNITY_STANDALONE_WIN) && DEVELOPMENT_BUILD)
        public const string dll    = "YBehavior";
#elif (UNITY_STANDALONE_OSX || UNITY_STANDALONE_WIN)
        public const string dll    = "YBehavior";
#else
        public const string dll = "YBehavior";
#endif
    }

    public partial class SharpHelper
    {
        public static LoadDataCallback LoadDataCallback { get; set; }

        public static LogCallback OnLogCallback { get; set; }
        public static LogCallback OnErrorCallback { get; set; }
        public static LogCallback OnThreadLogCallback { get; set; }
        public static LogCallback OnThreadErrorCallback { get; set; }

        static List<STreeNode> s_NodeList = new List<STreeNode>();
        public static void Init()
        {
            InitSharp(444);

            if (LoadDataCallback != null)
                RegisterLoadData(LoadDataCallback);

            RegisterLogCallback(
                OnLogCallback,
                OnErrorCallback,
                OnThreadLogCallback,
                OnThreadErrorCallback);

            var subTypeQuery = from t in System.Reflection.Assembly.GetExecutingAssembly().GetTypes()
                               where SUtility.IsSubClassOf(t, typeof(STreeNode))
                               select t;

            foreach (var type in subTypeQuery)
            {
                STreeNode node = Activator.CreateInstance(type) as STreeNode;
                node.Register();
                s_NodeList.Add(node);
            }
        }

        public static void Register<T>() where T : STreeNode
        {
            STreeNode node = Activator.CreateInstance<T>();
            node.Register();
            s_NodeList.Add(node);
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////
        [DllImport(VERSION.dll)]
        static public extern void InitSharp(int debugPort);

        [DllImport(VERSION.dll, CallingConvention = CallingConvention.StdCall)]
        static public extern void RegisterLogCallback(
            LogCallback log,
            LogCallback error,
            LogCallback threadlog,
            LogCallback threaderror
            );

        [DllImport(VERSION.dll)]
        static public extern IntPtr CreateEntity();

        [DllImport(VERSION.dll)]
        static public extern void DeleteEntity(IntPtr pEntity);

        [DllImport(VERSION.dll)]
        static public extern IntPtr CreateAgent(IntPtr pEntity);

        [DllImport(VERSION.dll)]
        static public extern void DeleteAgent(IntPtr pAgent);

        [DllImport(VERSION.dll, CallingConvention = CallingConvention.StdCall)]
        static public extern void RegisterSharpNode(
            string name,
            OnNodeLoaded onload,
            OnNodeUpdate onupdate);

        [DllImport(VERSION.dll, CallingConvention = CallingConvention.StdCall)]
        static public extern void RegisterLoadData(LoadDataCallback loaddata);

        [DllImport(VERSION.dll)]
        static public extern bool SetBehavior(
            IntPtr pAgent,
            string fsmname,
            string[] state2Tree, uint stSize,
            string[] tree2Tree, uint ttSize);

        [DllImport(VERSION.dll)]
        static public extern void Tick(IntPtr pAgent);

        [DllImport(VERSION.dll)]
        static public extern bool TryGetValue(
            IntPtr pNode,
            string attrName,
            IntPtr data);

        [DllImport(VERSION.dll)]
        static public extern IntPtr CreateVariable(
            IntPtr pNode,
            string attrName,
            IntPtr data,
            bool noConst);

        [DllImport(VERSION.dll)]
        static public extern IntPtr SetSharedDataByString(
            IntPtr pAgent,
            string name,
            string value,
            char separator = '|'
        );

        #region DEBUGGER
        [DllImport(VERSION.dll)]
        static public extern void LogVariable(IntPtr pNode, IntPtr pVariable, bool before);
        [DllImport(VERSION.dll)]
        static public extern bool HasDebugPoint(IntPtr pNode);
        [DllImport(VERSION.dll)]
        static public extern void LogInfo(IntPtr pNode, string info);
        #endregion

        [DllImport(VERSION.dll)]
        static public extern TYPEID GetVariableTypeID(IntPtr pVariable);
        [DllImport(VERSION.dll)]
        static public extern TYPEID GetVariableElementTypeID(IntPtr pVariable);
        [DllImport(VERSION.dll)]
        static public extern KEY GetTypeKeyByName(string name, TYPEID type);

        [DllImport(VERSION.dll)]
        static public extern TYPEID GetClassTypeNumberIdInt();
        [DllImport(VERSION.dll)]
        static public extern TYPEID GetClassTypeNumberIdUlong();
        [DllImport(VERSION.dll)]
        static public extern TYPEID GetClassTypeNumberIdFloat();
        [DllImport(VERSION.dll)]
        static public extern TYPEID GetClassTypeNumberIdString();
        [DllImport(VERSION.dll)]
        static public extern TYPEID GetClassTypeNumberIdBool();
        [DllImport(VERSION.dll)]
        static public extern TYPEID GetClassTypeNumberIdEntityWrapper();
        [DllImport(VERSION.dll)]
        static public extern TYPEID GetClassTypeNumberIdVector3();
        [DllImport(VERSION.dll)]
        static public extern TYPEID GetClassTypeNumberIdVecInt();
        [DllImport(VERSION.dll)]
        static public extern TYPEID GetClassTypeNumberIdVecUlong();
        [DllImport(VERSION.dll)]
        static public extern TYPEID GetClassTypeNumberIdVecFloat();
        [DllImport(VERSION.dll)]
        static public extern TYPEID GetClassTypeNumberIdVecString();
        [DllImport(VERSION.dll)]
        static public extern TYPEID GetClassTypeNumberIdVecBool();
        [DllImport(VERSION.dll)]
        static public extern TYPEID GetClassTypeNumberIdVecEntityWrapper();
        [DllImport(VERSION.dll)]
        static public extern TYPEID GetClassTypeNumberIdVecVector3();
    }
}
