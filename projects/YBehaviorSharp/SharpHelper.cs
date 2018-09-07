﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Linq;

namespace YBehaviorSharp
{
    using TYPEID = System.Int32;
    using INT = System.Int32;
    using BOOL = System.Int16;
    using FLOAT = System.Single;
    using ULONG = System.UInt64;
    using STRING = System.String;
    using Bool = System.Int16;

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
    public struct Vector3
	{
		public float x;
        public float y;
        public float z;
    }

    public struct EntityWrapper
    {
        IntPtr Core;
    }

    class GetClassType<T>
    {
        static TYPEID id = 0;
        public static TYPEID ID { get { return id; } }

        static GetClassType()
        {
            GetClassType<BOOL>.id = SharpHelper.GetClassTypeNumberIdBool();
            GetClassType<INT>.id = SharpHelper.GetClassTypeNumberIdInt();
            GetClassType<FLOAT>.id = SharpHelper.GetClassTypeNumberIdFloat();
            GetClassType<ULONG>.id = SharpHelper.GetClassTypeNumberIdUlong();
            GetClassType<STRING>.id = SharpHelper.GetClassTypeNumberIdString();
            GetClassType<Vector3>.id = SharpHelper.GetClassTypeNumberIdVector3();
            GetClassType<EntityWrapper>.id = SharpHelper.GetClassTypeNumberIdEntityWrapper();
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

        //////////////////////////////////////////////////////////////////////////////////////////////////////

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
        static public extern void SetTree(IntPtr pAgent, string treename);

        [DllImport(VERSION.dll)]
        static public extern void Tick(IntPtr pAgent);

        [DllImport(VERSION.dll)]
        static public extern IntPtr CreateVariable(
            IntPtr pNode,
            string attrName,
            IntPtr data,
            bool bSingle,
            char variableType);

        [DllImport(VERSION.dll)]
        static public extern TYPEID GetVariableTypeID(IntPtr pVariable);
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
    }
}
