using System;

namespace YBehaviorSharp
{
    public partial class SharpHelper
    {
        /// <summary>
        /// Get the variable by a key.
        /// You can get the key from SharpHelper.GetVariableKeyByName
        /// </summary>
        /// <param name="pAgent">Pointer to the agent in cpp</param>
        /// <param name="key">The variable key</param>
        /// <returns></returns>
        public static int GetSharedInt(IntPtr pAgent, int key)
        {
            SUtility.GetSharedVariableToBuffer(pAgent, key, GetType<int>.ID);
            return SUtility.GetFromBufferInt();
        }
        /// <summary>
        /// Get the variable by a key.
        /// You can get the key from SharpHelper.GetVariableKeyByName
        /// </summary>
        /// <param name="pAgent">Pointer to the agent in cpp</param>
        /// <param name="key">The variable key</param>
        /// <returns></returns>
        public static float GetSharedFloat(IntPtr pAgent, int key)
        {
            SUtility.GetSharedVariableToBuffer(pAgent, key, GetType<float>.ID);
            return SUtility.GetFromBufferFloat();
        }
        /// <summary>
        /// Get the variable by a key.
        /// You can get the key from SharpHelper.GetVariableKeyByName
        /// </summary>
        /// <param name="pAgent">Pointer to the agent in cpp</param>
        /// <param name="key">The variable key</param>
        /// <returns></returns>
        public static ulong GetSharedUlong(IntPtr pAgent, int key)
        {
            SUtility.GetSharedVariableToBuffer(pAgent, key, GetType<ulong>.ID);
            return SUtility.GetFromBufferUlong();
        }
        /// <summary>
        /// Get the variable by a key.
        /// You can get the key from SharpHelper.GetVariableKeyByName
        /// </summary>
        /// <param name="pAgent">Pointer to the agent in cpp</param>
        /// <param name="key">The variable key</param>
        /// <returns></returns>
        public static bool GetSharedBool(IntPtr pAgent, int key)
        {
            SUtility.GetSharedVariableToBuffer(pAgent, key, GetType<int>.ID);
            return SUtility.GetFromBufferBool() != 0;
        }
        /// <summary>
        /// Get the variable by a key.
        /// You can get the key from SharpHelper.GetVariableKeyByName
        /// </summary>
        /// <param name="pAgent">Pointer to the agent in cpp</param>
        /// <param name="key">The variable key</param>
        /// <returns></returns>
        public static Vector3 GetSharedVector3(IntPtr pAgent, int key)
        {
            SUtility.GetSharedVariableToBuffer(pAgent, key, GetType<Vector3>.ID);
            return SUtility.GetFromBufferVector3();
        }
        /// <summary>
        /// Get the variable by a key.
        /// You can get the key from SharpHelper.GetVariableKeyByName
        /// </summary>
        /// <param name="pAgent">Pointer to the agent in cpp</param>
        /// <param name="key">The variable key</param>
        /// <returns></returns>
        public static string GetSharedString(IntPtr pAgent, int key)
        {
            SUtility.GetSharedVariableToBuffer(pAgent, key, GetType<string>.ID);
            SUtility.GetFromBufferString(SUtility.CharBuffer, SUtility.CharBuffer.Length);
            return SUtility.BuildStringFromCharBuffer();
        }
        /// <summary>
        /// Get the variable by a key.
        /// You can get the key from SharpHelper.GetVariableKeyByName
        /// </summary>
        /// <param name="pAgent">Pointer to the agent in cpp</param>
        /// <param name="key">The variable key</param>
        /// <returns></returns>
        public static IEntity? GetSharedEntity(IntPtr pAgent, int key)
        {
            var index = SUtility.GetSharedEntityIndex(pAgent, key);
            //var ptr = SUtility.GetFromBufferEntity();
            return SPtrMgr.Instance.Get(index) as IEntity;
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Set the variable by a key
        /// </summary>
        /// <param name="pAgent">Pointer to the agent in cpp</param>
        /// <param name="key">The variable key</param>
        /// <param name="v"></param>
        public static void SetSharedInt(IntPtr pAgent, int key, int v)
        {
            SUtility.SetToBufferInt(v);
            SUtility.SetSharedVariable(pAgent, key, GetType<int>.ID);
        }
        /// <summary>
        /// Set the variable by a key
        /// </summary>
        /// <param name="pAgent">Pointer to the agent in cpp</param>
        /// <param name="key">The variable key</param>
        /// <param name="v"></param>
        public static void SetSharedFloat(IntPtr pAgent, int key, float v)
        {
            SUtility.SetToBufferFloat(v);
            SUtility.SetSharedVariable(pAgent, key, GetType<float>.ID);
        }
        /// <summary>
        /// Set the variable by a key
        /// </summary>
        /// <param name="pAgent">Pointer to the agent in cpp</param>
        /// <param name="key">The variable key</param>
        /// <param name="v"></param>
        public static void SetSharedUlong(IntPtr pAgent, int key, ulong v)
        {
            SUtility.SetToBufferUlong(v);
            SUtility.SetSharedVariable(pAgent, key, GetType<ulong>.ID);
        }
        /// <summary>
        /// Set the variable by a key
        /// </summary>
        /// <param name="pAgent">Pointer to the agent in cpp</param>
        /// <param name="key">The variable key</param>
        /// <param name="v"></param>
        public static void SetSharedBool(IntPtr pAgent, int key, bool v)
        {
            SUtility.SetToBufferBool(v ? (Byte)1 : (Byte)0);
            SUtility.SetSharedVariable(pAgent, key, GetType<int>.ID);
        }
        /// <summary>
        /// Set the variable by a key
        /// </summary>
        /// <param name="pAgent">Pointer to the agent in cpp</param>
        /// <param name="key">The variable key</param>
        /// <param name="v"></param>
        public static void SetSharedVector3(IntPtr pAgent, int key, Vector3 v)
        {
            SUtility.SetToBufferVector3(v);
            SUtility.SetSharedVariable(pAgent, key, GetType<Vector3>.ID);
        }
        /// <summary>
        /// Set the variable by a key
        /// </summary>
        /// <param name="pAgent">Pointer to the agent in cpp</param>
        /// <param name="key">The variable key</param>
        /// <param name="v"></param>
        public static void SetSharedString(IntPtr pAgent, int key, string v)
        {
            SharpHelper.SetToBufferString(v);
            SUtility.SetSharedVariable(pAgent, key, GetType<string>.ID);
        }
        /// <summary>
        /// Set the variable by a key
        /// </summary>
        /// <param name="pAgent">Pointer to the agent in cpp</param>
        /// <param name="key">The variable key</param>
        /// <param name="v"></param>
        public static void SetSharedEntity(IntPtr pAgent, int key, IEntity? v)
        {
            SUtility.SetToBufferEntity(v == null ? IntPtr.Zero : v.Ptr);
            SUtility.SetSharedVariable(pAgent, key, GetType<IEntity>.ID);
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////


        /// <summary>
        /// Get the array variable by a key.
        /// You can get the key from SharpHelper.GetVariableKeyByName
        /// </summary>
        /// <typeparam name="T">Element type</typeparam>
        /// <param name="pAgent">Pointer to the agent in cpp</param>
        /// <param name="key">The variable key</param>
        /// <returns>A new array object</returns>
        public static ISArray GetSharedArray<T>(IntPtr pAgent, int key)
        {
            return SArrayHelper.GetArray(SUtility.GetSharedVariablePtr(pAgent, key, GetType<T>.VecID), GetType<T>.ID);

        }
        /// <summary>
        /// Get the array variable by a key, and set to the array object.
        /// You can get the key from SharpHelper.GetVariableKeyByName
        /// </summary>
        /// <typeparam name="T">Element type</typeparam>
        /// <param name="pAgent">Pointer to the agent in cpp</param>
        /// <param name="key">The variable key</param>
        /// <param name="array">The variable value will be set to this object</param>
        /// <returns></returns>
        public static void GetSharedArray<T>(IntPtr pAgent, int key, SArray<T> array) where T : struct
        {
            array.Init(SUtility.GetSharedVariablePtr(pAgent, key, GetType<T>.VecID));

        }
        /// <summary>
        /// Get the array variable by a key, and set to the array object.
        /// You can get the key from SharpHelper.GetVariableKeyByName
        /// </summary>
        /// <typeparam name="T">Element type</typeparam>
        /// <param name="pAgent">Pointer to the agent in cpp</param>
        /// <param name="key">The variable key</param>
        /// <param name="array">The variable value will be set to this object</param>
        /// <returns></returns>
        public static void GetSharedArray<T>(IntPtr pAgent, int key, SArrayClass<T> array) where T : class
        {
            array.Init(SUtility.GetSharedVariablePtr(pAgent, key, GetType<T>.VecID));

        }
    }
}
