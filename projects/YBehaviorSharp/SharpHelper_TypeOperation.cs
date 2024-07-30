using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Linq;

namespace YBehaviorSharp
{
    using INT = System.Int32;
    using BOOL = System.Byte;
    using FLOAT = System.Single;
    using ULONG = System.UInt64;
    using STRING = System.String;
    using Bool = System.Byte;
    using KEY = System.Int32;

    public partial class SharpHelper
    {
        static public bool ConvertBool(BOOL b) { return b != 0; }
        static public BOOL ConvertBool(bool b) { return (BOOL)(b ? 1 : 0); }

        [DllImport(VERSION.dll)]
        static public extern int GetFromBufferInt();
        [DllImport(VERSION.dll)]
        static public extern void SetToBufferInt(int data);

        [DllImport(VERSION.dll)]
        static public extern float GetFromBufferFloat();
        [DllImport(VERSION.dll)]
        static public extern void SetToBufferFloat(float data);

        [DllImport(VERSION.dll)]
        static public extern ulong GetFromBufferUlong();
        [DllImport(VERSION.dll)]
        static public extern void SetToBufferUlong(ulong data);

        [DllImport(VERSION.dll)]
        static public extern Vector3 GetFromBufferVector3();
        [DllImport(VERSION.dll)]
        static public extern void SetToBufferVector3(Vector3 data);

        [DllImport(VERSION.dll)]
        static public extern BOOL GetFromBufferBool();
        [DllImport(VERSION.dll)]
        static public extern void SetToBufferBool(BOOL data);

        [DllImport(VERSION.dll)]
        static public extern IntPtr GetFromBufferEntity();
        [DllImport(VERSION.dll)]
        static public extern void SetToBufferEntity(IntPtr data);

        [DllImport(VERSION.dll)]
        static private extern void GetFromBufferString(StringBuilder sb, int len);
        static StringBuilder sb = new StringBuilder(100);
        static public string GetFromBufferString()
        {
            sb.Length = 0;
            GetFromBufferString(sb, sb.Capacity);
            return sb.ToString();
        }
        [DllImport(VERSION.dll)]
        static public extern void SetToBufferString(string data);

        [DllImport(VERSION.dll)]
        static public extern IntPtr GetFromBufferVector(int type);

        ///////////////////////////////////////////////////////////////

        [DllImport(VERSION.dll)]
        static public extern bool GetVariableValue(IntPtr pAgent, IntPtr pVariable);
        [DllImport(VERSION.dll)]
        static public extern IntPtr GetVariableValuePtr(IntPtr pAgent, IntPtr pVariable);
        [DllImport(VERSION.dll)]
        static public extern void SetVariableValue(IntPtr pAgent, IntPtr pVariable);


        ///////////////////////////////////////////////////////////////
        [DllImport(VERSION.dll)]
        static public extern void GetSharedData(IntPtr pAgent, int key, int type);
        [DllImport(VERSION.dll)]
        static public extern IntPtr GetSharedDataPtr(IntPtr pAgent, int key, int type);
        [DllImport(VERSION.dll)]
        static public extern void SetSharedData(IntPtr pAgent, int key, int type);


        ///////////////////////////////////////////////////////////////
        ///////////////////////////////////////////////////////////////
        [DllImport(VERSION.dll)]
        static public extern uint VectorGetSize(IntPtr pVector, int type);
        [DllImport(VERSION.dll)]
        static public extern void VectorClear(IntPtr pVector, int type);
        [DllImport(VERSION.dll)]
        static public extern void VectorPushBack(IntPtr pVector, int type);
        [DllImport(VERSION.dll)]
        static public extern bool VectorSet(IntPtr pVector, int index, int type);
        [DllImport(VERSION.dll)]
        static public extern bool VectorGet(IntPtr pVector, int index, int type);

        ///////////////////////////////////////////////////////////////
        ///////////////////////////////////////////////////////////////
        [DllImport(VERSION.dll)]
        static public extern int ToInt(IntPtr ptr);
        [DllImport(VERSION.dll)]
        static public extern float ToFloat(IntPtr ptr);
        [DllImport(VERSION.dll)]
        static public extern BOOL ToBool(IntPtr ptr);
        [DllImport(VERSION.dll)]
        static public extern ulong ToUlong(IntPtr ptr);
        [DllImport(VERSION.dll)]
        static public extern Vector3 ToVector3(IntPtr ptr);
        [DllImport(VERSION.dll)]
        static public extern IntPtr ToEntity(IntPtr ptr);
        [DllImport(VERSION.dll)]
        static private extern bool ToString(IntPtr ptr, StringBuilder sb, int len);
        static public string ToString(IntPtr ptr)
        {
            sb.Length = 0;
            if (ToString(ptr, sb, sb.Capacity))
                return sb.ToString();
            return string.Empty;
        }

    }
}
