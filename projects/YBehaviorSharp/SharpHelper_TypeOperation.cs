﻿using System;
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

    internal partial class SUtility
    {
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
        static public extern void GetFromBufferString(byte[] s, int len);

        [DllImport(VERSION.dll)]
        static public extern IntPtr GetFromBufferArray(int type);

        [DllImport(VERSION.dll)]
        static public extern bool GetPinValue(IntPtr pAgent, IntPtr pPin);
        [DllImport(VERSION.dll)]
        static public extern IntPtr GetPinValuePtr(IntPtr pAgent, IntPtr pPin);
        [DllImport(VERSION.dll)]
        static public extern void SetPinValue(IntPtr pAgent, IntPtr pPin);

        [DllImport(VERSION.dll)]
        static public extern int GetPinEntityIndex(IntPtr pAgent, IntPtr pPin);


        [DllImport(VERSION.dll)]
        static public extern bool GetSharedVariableToBuffer(IntPtr pAgent, int key, int type);
        [DllImport(VERSION.dll)]
        static public extern IntPtr GetSharedVariablePtr(IntPtr pAgent, int key, int type);
        [DllImport(VERSION.dll)]
        static public extern void SetSharedVariable(IntPtr pAgent, int key, int type);
        [DllImport(VERSION.dll)]
        static public extern int GetSharedEntityIndex(IntPtr pAgent, int key);


        [DllImport(VERSION.dll)]
        static public extern uint ArrayGetSize(IntPtr pVector, int type);
        [DllImport(VERSION.dll)]
        static public extern void ArrayClear(IntPtr pVector, int type);
        [DllImport(VERSION.dll)]
        static public extern void ArrayPushBack(IntPtr pVector, int type);
        [DllImport(VERSION.dll)]
        static public extern bool ArraySet(IntPtr pVector, int index, int type);
        [DllImport(VERSION.dll)]
        static public extern bool ArrayGet(IntPtr pVector, int index, int type);
        [DllImport(VERSION.dll)]
        static public extern bool ArrayErase(IntPtr pVector, int type);
        [DllImport(VERSION.dll)]
        static public extern bool ArrayEraseAt(IntPtr pVector, int index, int type);
        [DllImport(VERSION.dll)]
        static public extern int ArrayFind(IntPtr pVector, int type);
        [DllImport(VERSION.dll)]
        static public extern int ArrayGetEntityIndex(IntPtr pVector, int index);



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
        static public extern bool ToString(IntPtr ptr, char[] s, int len);
    }
    public partial class SharpHelper
    {
        /// <summary>
        /// Convert from special bool to normal bool
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        static public bool ConvertBool(BOOL b) { return b != 0; }
        /// <summary>
        /// Convert from normal bool to special bool
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        static public BOOL ConvertBool(bool b) { return (BOOL)(b ? 1 : 0); }
        /// <summary>
        /// Get the string cpp put in the buffer
        /// </summary>
        /// <returns></returns>
        public static unsafe string GetFromBufferString()
        {
            SUtility.GetFromBufferString(SUtility.CharBuffer, SUtility.CharBuffer.Length);
            return SUtility.BuildStringFromCharBuffer();
        }
        /// <summary>
        /// Put the string to the buffer, to let the cpp fetch
        /// </summary>
        /// <param name="data"></param>
        [DllImport(VERSION.dll)]
        static public extern void SetToBufferString(string? data);
    }
}
