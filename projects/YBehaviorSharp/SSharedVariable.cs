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
        /// <param name="output">The value of the variable</param>
        /// <returns>True if the variable exists</returns>
        public static bool GetSharedVariable(IntPtr pAgent, int key, out int output)
        {
            if (SUtility.GetSharedVariableToBuffer(pAgent, key, GetType<int>.ID))
            {
                output = SUtility.GetFromBufferInt();
                return true;
            }
            else
            {
                output = 0;
                return false;
            }
        }
        /// <summary>
        /// Get the variable by a key.
        /// You can get the key from SharpHelper.GetVariableKeyByName
        /// </summary>
        /// <param name="pAgent">Pointer to the agent in cpp</param>
        /// <param name="key">The variable key</param>
        /// <param name="output">The value of the variable</param>
        /// <returns>True if the variable exists</returns>
        public static bool GetSharedVariable(IntPtr pAgent, int key, out float output)
        {
            if (SUtility.GetSharedVariableToBuffer(pAgent, key, GetType<float>.ID))
            {
                output = SUtility.GetFromBufferFloat();
                return true;
            }
            else
            {
                output = 0f;
                return false; 
            }
        }
        /// <summary>
        /// Get the variable by a key.
        /// You can get the key from SharpHelper.GetVariableKeyByName
        /// </summary>
        /// <param name="pAgent">Pointer to the agent in cpp</param>
        /// <param name="key">The variable key</param>
        /// <param name="output">The value of the variable</param>
        /// <returns>True if the variable exists</returns>
        public static bool GetSharedVariable(IntPtr pAgent, int key, out ulong output)
        {
            if (SUtility.GetSharedVariableToBuffer(pAgent, key, GetType<ulong>.ID))
            {
                output = SUtility.GetFromBufferUlong();
                return true;
            }
            else
            {
                output = 0;
                return false;
            }
        }
        /// <summary>
        /// Get the variable by a key.
        /// You can get the key from SharpHelper.GetVariableKeyByName
        /// </summary>
        /// <param name="pAgent">Pointer to the agent in cpp</param>
        /// <param name="key">The variable key</param>
        /// <param name="output">The value of the variable</param>
        /// <returns>True if the variable exists</returns>
        public static bool GetSharedVariable(IntPtr pAgent, int key, out bool output)
        {
            if (SUtility.GetSharedVariableToBuffer(pAgent, key, GetType<bool>.ID))
            {
                output = SUtility.GetFromBufferBool() != 0;
                return true;
            }
            else
            {
                output = false;
                return false;
            }
        }
        /// <summary>
        /// Get the variable by a key.
        /// You can get the key from SharpHelper.GetVariableKeyByName
        /// </summary>
        /// <param name="pAgent">Pointer to the agent in cpp</param>
        /// <param name="key">The variable key</param>
        /// <param name="output">The value of the variable</param>
        /// <returns>True if the variable exists</returns>
        public static bool GetSharedVariable(IntPtr pAgent, int key, out Vector3 output)
        {
            if (SUtility.GetSharedVariableToBuffer(pAgent, key, GetType<Vector3>.ID))
            {
                output = SUtility.GetFromBufferVector3();
                return true;
            }
            else
            {
                output = Vector3.zero;
                return false;
            }
        }
        /// <summary>
        /// Get the variable by a key.
        /// You can get the key from SharpHelper.GetVariableKeyByName
        /// </summary>
        /// <param name="pAgent">Pointer to the agent in cpp</param>
        /// <param name="key">The variable key</param>
        /// <param name="output">The value of the variable</param>
        /// <returns>True if the variable exists</returns>
        public static bool GetSharedVariable(IntPtr pAgent, int key, out string output)
        {
            if (SUtility.GetSharedVariableToBuffer(pAgent, key, GetType<string>.ID))
            {
                SUtility.GetFromBufferString(SUtility.CharBuffer, SUtility.CharBuffer.Length);
                output = SUtility.BuildStringFromCharBuffer();
                return true;
            }
            else
            {
                output = string.Empty;
                return false;
            }
        }
        /// <summary>
        /// Get the variable by a key.
        /// You can get the key from SharpHelper.GetVariableKeyByName
        /// </summary>
        /// <param name="pAgent">Pointer to the agent in cpp</param>
        /// <param name="key">The variable key</param>
        /// <param name="output">The value of the variable</param>
        /// <returns>True if the variable exists</returns>
        public static bool GetSharedVariable(IntPtr pAgent, int key, out IEntity? output)
        {
            var index = SUtility.GetSharedEntityIndex(pAgent, key);
            output = SPtrMgr.Instance.Get(index) as IEntity;
            return output != null;
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Set the variable by a key
        /// </summary>
        /// <param name="pAgent">Pointer to the agent in cpp</param>
        /// <param name="key">The variable key</param>
        /// <param name="v"></param>
        public static void SetSharedVariable(IntPtr pAgent, int key, int v)
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
        public static void SetSharedVariable(IntPtr pAgent, int key, float v)
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
        public static void SetSharedVariable(IntPtr pAgent, int key, ulong v)
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
        public static void SetSharedVariable(IntPtr pAgent, int key, bool v)
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
        public static void SetSharedVariable(IntPtr pAgent, int key, Vector3 v)
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
        public static void SetSharedVariable(IntPtr pAgent, int key, string v)
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
        public static void SetSharedVariable(IntPtr pAgent, int key, IEntity? v)
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
