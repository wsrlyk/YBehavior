using System;

namespace YBehaviorSharp
{
    public class SSharedData
    {
        public static int GetSharedInt(IntPtr pAgent, int key)
        {
            SharpHelper.GetSharedData(pAgent, key, GetClassType<int>.ID);
            return SharpHelper.GetFromBufferInt();
        }
        public static float GetSharedFloat(IntPtr pAgent, int key)
        {
            SharpHelper.GetSharedData(pAgent, key, GetClassType<float>.ID);
            return SharpHelper.GetFromBufferFloat();
        }
        public static ulong GetSharedUlong(IntPtr pAgent, int key)
        {
            SharpHelper.GetSharedData(pAgent, key, GetClassType<ulong>.ID);
            return SharpHelper.GetFromBufferUlong();
        }
        public static bool GetSharedBool(IntPtr pAgent, int key)
        {
            SharpHelper.GetSharedData(pAgent, key, GetClassType<int>.ID);
            return SharpHelper.GetFromBufferBool() != 0;
        }
        public static Vector3 GetSharedVector3(IntPtr pAgent, int key)
        {
            SharpHelper.GetSharedData(pAgent, key, GetClassType<Vector3>.ID);
            return SharpHelper.GetFromBufferVector3();
        }
        public static string GetSharedString(IntPtr pAgent, int key)
        {
            SharpHelper.GetSharedData(pAgent, key, GetClassType<string>.ID);
            SharpHelper.GetFromBufferString(SUtility.CharBuffer, SUtility.CharBuffer.Length);
            return SUtility.BuildStringFromCharBuffer();
        }
        public static IntPtr GetSharedEntity(IntPtr pAgent, int key)
        {
            SharpHelper.GetSharedData(pAgent, key, GetClassType<SEntity>.ID);
            return SharpHelper.GetFromBufferEntity();
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////

        public static void SetSharedInt(IntPtr pAgent, int key, int v)
        {
            SharpHelper.SetToBufferInt(v);
            SharpHelper.SetSharedData(pAgent, key, GetClassType<int>.ID);
        }
        public static void SetSharedFloat(IntPtr pAgent, int key, float v)
        {
            SharpHelper.SetToBufferFloat(v);
            SharpHelper.SetSharedData(pAgent, key, GetClassType<float>.ID);
        }
        public static void SetSharedUlong(IntPtr pAgent, int key, ulong v)
        {
            SharpHelper.SetToBufferUlong(v);
            SharpHelper.SetSharedData(pAgent, key, GetClassType<ulong>.ID);
        }
        public static void SetSharedBool(IntPtr pAgent, int key, bool v)
        {
            SharpHelper.SetToBufferBool(v ? (Byte)1 : (Byte)0);
            SharpHelper.SetSharedData(pAgent, key, GetClassType<int>.ID);
        }
        public static void SetSharedVector3(IntPtr pAgent, int key, Vector3 v)
        {
            SharpHelper.SetToBufferVector3(v);
            SharpHelper.SetSharedData(pAgent, key, GetClassType<Vector3>.ID);
        }
        public static void SetSharedString(IntPtr pAgent, int key, string v)
        {
            SharpHelper.SetToBufferString(v);
            SharpHelper.SetSharedData(pAgent, key, GetClassType<string>.ID);
        }
        public static void SetSharedEntity(IntPtr pAgent, int key, IntPtr v)
        {
            SharpHelper.SetToBufferEntity(v);
            SharpHelper.SetSharedData(pAgent, key, GetClassType<SEntity>.ID);
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////

        public static ISArray GetSharedArray<T>(IntPtr pAgent, int key)
        {
            SharpHelper.GetSharedData(pAgent, key, GetClassType<T>.VecID);
            return SArrayHelper.GetArray(SharpHelper.GetFromBufferArray(GetClassType<T>.VecID), GetClassType<T>.ID);

        }

    }
}
