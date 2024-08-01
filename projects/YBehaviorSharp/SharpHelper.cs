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
    using BOOL = System.Byte;
    using FLOAT = System.Single;
    using ULONG = System.UInt64;
    using STRING = System.String;
    using Bool = System.Byte;

    public delegate bool OnNodeLoaded(IntPtr pNode, IntPtr pData, int index);
    public delegate NodeState OnNodeUpdate(IntPtr pNode, IntPtr pAgent, int index);
    public delegate string LoadDataCallback(string treeName);
    public delegate void LogCallback();
    public delegate void GetFilePathCallback();

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
            GetClassType<IEntity>.id = SharpHelper.GetClassTypeNumberIdEntityWrapper();

            GetClassType<BOOL>.vecid = SharpHelper.GetClassTypeNumberIdVecBool();
            GetClassType<bool>.vecid = SharpHelper.GetClassTypeNumberIdVecBool();
            GetClassType<INT>.vecid = SharpHelper.GetClassTypeNumberIdVecInt();
            GetClassType<FLOAT>.vecid = SharpHelper.GetClassTypeNumberIdVecFloat();
            GetClassType<ULONG>.vecid = SharpHelper.GetClassTypeNumberIdVecUlong();
            GetClassType<STRING>.vecid = SharpHelper.GetClassTypeNumberIdVecString();
            GetClassType<Vector3>.vecid = SharpHelper.GetClassTypeNumberIdVecVector3();
            GetClassType<IEntity>.vecid = SharpHelper.GetClassTypeNumberIdVecEntityWrapper();
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
    public interface ISharpLauncher
    {
        int DebugPort { get; }
        void OnLog();
        void OnError();

        void OnGetFilePath();
    }

    public partial class SharpHelper
    {
        public static void Init(ISharpLauncher launcher)
        {
            InitSharp(launcher.DebugPort);

            RegisterLogCallback(
                launcher.OnLog,
                launcher.OnError,
                launcher.OnLog,
                launcher.OnError);
            RegisterGetFilePathCallback(launcher.OnGetFilePath);

            //var subTypeQuery = from t in System.Reflection.Assembly.GetExecutingAssembly().GetTypes()
            //                   where SUtility.IsSubClassOf(t, typeof(STreeNode))
            //                   select t;

            //foreach (var type in subTypeQuery)
            //{
            //    STreeNode node = Activator.CreateInstance(type) as STreeNode;
            //    node.Register();
            //    s_NodeList.Add(node);
            //}
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////
        [DllImport(VERSION.dll)]
        static extern void InitSharp(int debugPort);

        [DllImport(VERSION.dll)]
        static extern void RegisterGetFilePathCallback(GetFilePathCallback callback);

        [DllImport(VERSION.dll, CallingConvention = CallingConvention.StdCall)]
        static extern void RegisterLogCallback(
            LogCallback log,
            LogCallback error,
            LogCallback threadlog,
            LogCallback threaderror
            );

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
