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

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int OnNodeLoaded(IntPtr pNode, IntPtr pData, int staticIndex);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate ENodeState OnNodeUpdate(IntPtr pNode, IntPtr pAgent, int agentIndex, int staticIndex, int dynamicIndex);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void OnNodeContextInit(IntPtr pNode, int staticIndex, int dynamicIndex, uint contextUID);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate ENodeState OnNodeContextUpdate(IntPtr pNode, IntPtr pAgent, int agentIndex, int staticIndex, int dynamicIndex, uint contextUID, ENodeState lastState);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void LogCallback();
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void GetFilePathCallback();
#if YDEBUGGER
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void OnDebugStateChangedCallback(bool isDebugging);
#endif
    /// <summary>
    /// Running state of tree node
    /// </summary>
    public enum ENodeState
    {
        /// <summary>
        /// Error
        /// </summary>
        Invalid = -1,
        /// <summary>
        /// Return successfully
        /// </summary>
        Success,
        /// <summary>
        /// Return with failure
        /// </summary>
        Failure,
        /// <summary>
        /// Hit a break point
        /// </summary>
        Break,
        /// <summary>
        /// Child hit a break point, or it's still working
        /// </summary>
        Running,
    };
    /// <summary>
    /// A simple Vector3
    /// </summary>
    public struct Vector3
	{
        /// <summary>
        /// x-axis
        /// </summary>
		public float x;
        /// <summary>
        /// y-axis
        /// </summary>
        public float y;
        /// <summary>
        /// z-axis
        /// </summary>
        public float z;
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="_x"></param>
        /// <param name="_y"></param>
        /// <param name="_z"></param>
        public Vector3(float _x, float _y, float _z)
        {
            x = _x; y = _y; z = _z;
        }
        /// <summary>
        /// Zero Vector3
        /// </summary>
        public static Vector3 zero = new Vector3(0f, 0f, 0f);
    }
    /// <summary>
    /// Get the TypeID of a type
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class GetType<T>
    {
        static TYPEID id = -1;
        static TYPEID vecid = -1;
        /// <summary>
        /// The TypeID of single type
        /// </summary>
        public static TYPEID ID { get { return id; } }
        /// <summary>
        /// The TypeID of array type
        /// </summary>
        public static TYPEID VecID { get { return vecid; } }

        static GetType()
        {
            GetType<BOOL>.id = SUtility.GetTypeIdBool();
            GetType<bool>.id = SUtility.GetTypeIdBool();
            GetType<INT>.id = SUtility.GetTypeIdInt();
            GetType<FLOAT>.id = SUtility.GetTypeIdFloat();
            GetType<ULONG>.id = SUtility.GetTypeIdUlong();
            GetType<STRING>.id = SUtility.GetTypeIdString();
            GetType<Vector3>.id = SUtility.GetTypeIdVector3();
            GetType<IEntity>.id = SUtility.GetTypeIdEntityWrapper();

            GetType<BOOL>.vecid = SUtility.GetTypeIdVecBool();
            GetType<bool>.vecid = SUtility.GetTypeIdVecBool();
            GetType<INT>.vecid = SUtility.GetTypeIdVecInt();
            GetType<FLOAT>.vecid = SUtility.GetTypeIdVecFloat();
            GetType<ULONG>.vecid = SUtility.GetTypeIdVecUlong();
            GetType<STRING>.vecid = SUtility.GetTypeIdVecString();
            GetType<Vector3>.vecid = SUtility.GetTypeIdVecVector3();
            GetType<IEntity>.vecid = SUtility.GetTypeIdVecEntityWrapper();
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
    /// <summary>
    /// Interface of class for initializing the system
    /// </summary>
    public interface ISharpLauncher
    {
        /// <summary>
        /// Port to debug
        /// </summary>
        int DebugPort { get; }
        /// <summary>
        /// Log callback
        /// The string can be fetched by SharpHelper.GetFromBufferString;
        /// </summary>
        void OnLog();
        /// <summary>
        /// Error callback.
        /// The string can be fetched by SharpHelper.GetFromBufferString;
        /// </summary>
        void OnError();
        /// <summary>
        /// Get the file path.
        /// File name can be fetched by SharpHelper.GetFromBufferString, 
        /// and the path should be saved by SharpHelper.SetToBufferString
        /// </summary>
        void OnGetFilePath();
    }

    /// <summary>
    /// Utilities
    /// </summary>
    public partial class SharpHelper
    {
        static LogCallback? s_onLog;
        static LogCallback? s_onError;
        static GetFilePathCallback? s_onGetFilePath;
        /// <summary>
        /// Init the system. Should be called at first
        /// </summary>
        /// <param name="launcher"></param>
        public static void Init(ISharpLauncher launcher)
        {
            SUtility.InitSharp(launcher.DebugPort);

            s_onLog = launcher.OnLog;
            s_onError = launcher.OnError;
            s_onGetFilePath = launcher.OnGetFilePath;

            SUtility.RegisterLogCallback(
                s_onLog,
                s_onError,
                s_onLog,
                s_onError);
            SUtility.RegisterGetFilePathCallback(s_onGetFilePath);

        }

        /// <summary>
        /// Uninit the system. Should be call at the end of game
        /// </summary>
        [DllImport(VERSION.dll)]
        static public extern void UninitSharp();

        /// <summary>
        /// Set the behavior
        /// </summary>
        /// <param name="pAgent">Pointer to the agent in cpp</param>
        /// <param name="fsmname">FSM name</param>
        /// <param name="state2Tree">A mapping from state name to tree name, [state0, tree0, state1, tree1, ...]</param>
        /// <param name="stSize">Length of state2Tree</param>
        /// <param name="tree2Tree">A mapping from parent tree to subtree, [parent0, child0, parent1, child1, ...]</param>
        /// <param name="ttSize">Length of tree2Tree</param>
        /// <returns></returns>
        [DllImport(VERSION.dll)]
        static public extern bool SetBehavior(
            IntPtr pAgent,
            string fsmname,
            string[] state2Tree, uint stSize,
            string[] tree2Tree, uint ttSize);
        /// <summary>
        /// Tick the agent
        /// </summary>
        /// <param name="pAgent"></param>
        [DllImport(VERSION.dll)]
        static public extern void Tick(IntPtr pAgent);
        /// <summary>
        /// Get the data from config
        /// </summary>
        /// <param name="pNode">Pointer to the tree node in cpp</param>
        /// <param name="attrName">Attribute name in xml</param>
        /// <param name="data">Pointer to the xml data in cpp</param>
        /// <returns>If true, the data can be fetch by SharpHelper.GetFromBufferString</returns>
        [DllImport(VERSION.dll)]
        static public extern bool TryGetValue(
            IntPtr pNode,
            string attrName,
            IntPtr data);
        /// <summary>
        /// Set the shared variable
        /// </summary>
        /// <param name="pAgent">Pointer to the agent in cpp</param>
        /// <param name="name">Variable name</param>
        /// <param name="value">Variable value in string</param>
        /// <param name="separator">Separator for an array</param>
        /// <returns>Pointer to the variable</returns>
        [DllImport(VERSION.dll)]
        static public extern IntPtr SetSharedVariableByString(
            IntPtr pAgent,
            string name,
            string value,
            char separator = '|'
        );

        #region YDEBUGGER
        /// <summary>
        /// Try to log the value of pin when debugging.
        /// Only needed in TreeContext
        /// </summary>
        /// <param name="pNode">Pointer to the tree node in cpp</param>
        /// <param name="pin">The pin object</param>
        /// <param name="before">If true, the log will be put in the front of the debug information; otherwise, in the end of it</param>
        public static void TryLogPin(IntPtr pNode, SPin pin, bool before)
        {
#if YDEBUGGER
            if (pin == null) return;
            SUtility.LogPin(pNode, pin.Ptr, before);
#endif
        }
        /// <summary>
        /// Try to log some information when debugging
        /// </summary>
        /// <param name="pNode">Pointer to the tree node in cpp</param>
        /// <param name="info"></param>
        public static void TryLogInfo(IntPtr pNode, string info)
        {
#if YDEBUGGER
            if (SUtility.IsDebugging)
                SUtility.LogInfo(pNode, info);
#endif
        }
        /// <summary>
        /// Is connecting an editor and debugging
        /// </summary>
        public static bool IsDebugging =>
#if YDEBUGGER
            SUtility.IsDebugging;
#else
            false;
#endif


#endregion
        /// <summary>
        /// Make an error log with node title and name
        /// </summary>
        /// <param name="pNode">Pointer to the tree node in cpp</param>
        /// <param name="info">error content</param>
        [DllImport(VERSION.dll)]
        static public extern void NodeError(IntPtr pNode, string info);
        /// <summary>
        /// Get or create the ID of the variable by its name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        [DllImport(VERSION.dll)]
        static public extern KEY GetOrCreateVariableKeyByName(string name);

    }

    internal partial class SUtility
    {
        static SUtility()
        {
#if YDEBUGGER
            s_onDebugStateChanged = OnDebugStateChanged;
            RegisterOnDebugStateChangedCallback(s_onDebugStateChanged);
#endif
        }
        [DllImport(VERSION.dll)]
        static public extern TYPEID GetTypeIdInt();
        [DllImport(VERSION.dll)]
        static public extern TYPEID GetTypeIdUlong();
        [DllImport(VERSION.dll)]
        static public extern TYPEID GetTypeIdFloat();
        [DllImport(VERSION.dll)]
        static public extern TYPEID GetTypeIdString();
        [DllImport(VERSION.dll)]
        static public extern TYPEID GetTypeIdBool();
        [DllImport(VERSION.dll)]
        static public extern TYPEID GetTypeIdEntityWrapper();
        [DllImport(VERSION.dll)]
        static public extern TYPEID GetTypeIdVector3();
        [DllImport(VERSION.dll)]
        static public extern TYPEID GetTypeIdVecInt();
        [DllImport(VERSION.dll)]
        static public extern TYPEID GetTypeIdVecUlong();
        [DllImport(VERSION.dll)]
        static public extern TYPEID GetTypeIdVecFloat();
        [DllImport(VERSION.dll)]
        static public extern TYPEID GetTypeIdVecString();
        [DllImport(VERSION.dll)]
        static public extern TYPEID GetTypeIdVecBool();
        [DllImport(VERSION.dll)]
        static public extern TYPEID GetTypeIdVecEntityWrapper();
        [DllImport(VERSION.dll)]
        static public extern TYPEID GetTypeIdVecVector3();


        [DllImport(VERSION.dll)]
        static public extern void InitSharp(int debugPort);

        [DllImport(VERSION.dll)]
        static public extern void RegisterGetFilePathCallback(GetFilePathCallback callback);

        [DllImport(VERSION.dll, CallingConvention = CallingConvention.StdCall)]
        static public extern void RegisterLogCallback(
            LogCallback log,
            LogCallback error,
            LogCallback threadlog,
            LogCallback threaderror
            );


        [DllImport(VERSION.dll)]
        static public extern IntPtr CreatePin(
            IntPtr pNode,
            string attrName,
            IntPtr data,
            int flag);

        [DllImport(VERSION.dll)]
        static public extern TYPEID GetPinTypeID(IntPtr pPin);
        [DllImport(VERSION.dll)]
        static public extern TYPEID GetPinElementTypeID(IntPtr pPin);
        [DllImport(VERSION.dll)]
        static public extern bool IsPinConst(IntPtr pPin);

#if YDEBUGGER
        [DllImport(VERSION.dll)]
        static public extern void LogPin(IntPtr pNode, IntPtr pPin, bool before);
        [DllImport(VERSION.dll)]
        static public extern bool HasDebugPoint(IntPtr pNode);
        [DllImport(VERSION.dll)]
        static public extern void LogInfo(IntPtr pNode, string info);
        [DllImport(VERSION.dll)]
        static public extern void RegisterOnDebugStateChangedCallback(OnDebugStateChangedCallback callback);

        static OnDebugStateChangedCallback s_onDebugStateChanged;
        public static bool IsDebugging { get; private set; }
        static void OnDebugStateChanged(bool isDebugging)
        {
            IsDebugging = isDebugging;
        }
#endif

    }
}
