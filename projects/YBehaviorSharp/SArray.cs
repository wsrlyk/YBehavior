using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Linq;

namespace YBehaviorSharp
{
    using TYPEID = System.Int32;
    using BOOL = System.Byte;
    /// <summary>
    /// Interface of array data
    /// </summary>
    public interface ISArray
    {
        /// <summary>
        /// Init the array data
        /// </summary>
        /// <param name="core">Pointer to the std::vector in cpp</param>
        void Init(IntPtr core);
        /// <summary>
        /// Clear the elements
        /// </summary>
        void Clear();
        /// <summary>
        /// Get the count of elements
        /// </summary>
        /// <returns></returns>
        int GetLength();
    }
    /// <summary>
    /// Template class of array
    /// </summary>
    /// <typeparam name="T">Element type</typeparam>
    abstract public class SArrayBase<T> : ISArray
    {
        protected IntPtr m_Core;
        protected TYPEID m_ElementID;
        public SArrayBase(IntPtr core)
        {
            m_ElementID = GetType<T>.ID;
            Init(core);
        }
        public void Init(IntPtr core)
        {
            m_Core = core;
        }
        public void Clear()
        {
            SUtility.ArrayClear(m_Core, m_ElementID);
        }

        public int GetLength()
        {
            return (int)SUtility.ArrayGetSize(m_Core, m_ElementID);
        }
        /// <summary>
        /// Is pointing to the same array?
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool IsTheSame(SArrayBase<T> other)
        {
            return other.m_Core == m_Core && other.m_ElementID == m_ElementID;
        }
        /// <summary>
        /// Erase an element
        /// </summary>
        /// <param name="index">position</param>
        /// <returns>Whether it's successfully erased</returns>
        public bool EraseAt(int index)
        {
            return SUtility.ArrayEraseAt(m_Core, index, m_ElementID);
        }
    }
    abstract public class SArray<T> : SArrayBase<T> where T : struct
    {
        public SArray(IntPtr core) : base (core)
        {
        }
        /// <summary>
        /// Push a new element
        /// </summary>
        /// <param name="data"></param>
        abstract public void PushBack(T data);
        /// <summary>
        /// Set the value at a position
        /// </summary>
        /// <param name="data"></param>
        /// <param name="idx">position</param>
        abstract public void Set(T data, int idx);
        /// <summary>
        /// Get the value at a position
        /// </summary>
        /// <param name="idx">position</param>
        /// <returns></returns>
        abstract public T Get(int idx);
        //abstract public bool TryFind(T v, out int index);
    }

    abstract public class SArrayClass<T> : SArrayBase<T> where T : class
    {
        public SArrayClass(IntPtr core) : base(core)
        {
        }
        /// <summary>
        /// Push a new element
        /// </summary>
        /// <param name="data"></param>
        abstract public void PushBack(T? data);
        /// <summary>
        /// Set the value at a position
        /// </summary>
        /// <param name="data"></param>
        /// <param name="idx">position</param>
        abstract public void Set(T? data, int idx);
        /// <summary>
        /// Get the value at a position
        /// </summary>
        /// <param name="idx">position</param>
        /// <returns></returns>
        abstract public T? Get(int idx);
        //abstract public bool TryFind(T v, out int index);
    }

    internal class SArrayHelper
    {
        public static ISArray GetArray(IntPtr ptr, TYPEID elementType)
        {
            var t = s_ArrayTypes[elementType];
            return Activator.CreateInstance(t, ptr) as ISArray;
        }

        static System.Type[] s_ArrayTypes = new Type[7];
        static SArrayHelper()
        {
            s_ArrayTypes[GetType<int>.ID] = typeof(SArrayInt);
            s_ArrayTypes[GetType<float>.ID] = typeof(SArrayFloat);
            s_ArrayTypes[GetType<ulong>.ID] = typeof(SArrayUlong);
            s_ArrayTypes[GetType<bool>.ID] = typeof(SArrayBool);
            s_ArrayTypes[GetType<Vector3>.ID] = typeof(SArrayVector3);
            s_ArrayTypes[GetType<string>.ID] = typeof(SArrayString);
            s_ArrayTypes[GetType<IEntity>.ID] = typeof(SArrayEntity);
        }
    }
    ////////////////////////////////////////////////////////////////
    /// <summary>
    /// Int array
    /// </summary>
    public class SArrayInt : SArray<int>
    {
        public SArrayInt(IntPtr core) : base(core) { }
        public override void PushBack(int data)
        {
            SUtility.SetToBufferInt(data);
            SUtility.ArrayPushBack(m_Core, m_ElementID);
        }

        public override void Set(int data, int idx)
        {
            SUtility.SetToBufferInt(data);
            SUtility.ArraySet(m_Core, idx, m_ElementID);
        }

        public override int Get(int idx)
        {
            SUtility.ArrayGet(m_Core, idx, m_ElementID);
            return SUtility.GetFromBufferInt();
        }
    }
    /// <summary>
    /// Float array
    /// </summary>
    public class SArrayFloat : SArray<float>
    {
        public SArrayFloat(IntPtr core) : base(core) { }
        public override void PushBack(float data)
        {
            SUtility.SetToBufferFloat(data);
            SUtility.ArrayPushBack(m_Core, m_ElementID);
        }

        public override void Set(float data, int idx)
        {
            SUtility.SetToBufferFloat(data);
            SUtility.ArraySet(m_Core, idx, m_ElementID);
        }

        public override float Get(int idx)
        {
            SUtility.ArrayGet(m_Core, idx, m_ElementID);
            return SUtility.GetFromBufferFloat();
        }
    }
    /// <summary>
    /// Ulong array
    /// </summary>
    public class SArrayUlong : SArray<ulong>
    {
        public SArrayUlong(IntPtr core) : base(core) { }
        public override void PushBack(ulong data)
        {
            SUtility.SetToBufferUlong(data);
            SUtility.ArrayPushBack(m_Core, m_ElementID);
        }

        public override void Set(ulong data, int idx)
        {
            SUtility.SetToBufferUlong(data);
            SUtility.ArraySet(m_Core, idx, m_ElementID);
        }

        public override ulong Get(int idx)
        {
            SUtility.ArrayGet(m_Core, idx, m_ElementID);
            return SUtility.GetFromBufferUlong();
        }
    }
    /// <summary>
    /// Bool array
    /// </summary>
    public class SArrayBool : SArray<bool>
    {
        public SArrayBool(IntPtr core) : base(core) { }
        public override void PushBack(bool data)
        {
            SUtility.SetToBufferBool(SharpHelper.ConvertBool(data));
            SUtility.ArrayPushBack(m_Core, m_ElementID);
        }

        public override void Set(bool data, int idx)
        {
            SUtility.SetToBufferBool(SharpHelper.ConvertBool(data));
            SUtility.ArraySet(m_Core, idx, m_ElementID);
        }

        public override bool Get(int idx)
        {
            SUtility.ArrayGet(m_Core, idx, m_ElementID);
            return SharpHelper.ConvertBool(SUtility.GetFromBufferBool());
        }
    }
    /// <summary>
    /// Vector3 array
    /// </summary>
    public class SArrayVector3 : SArray<Vector3>
    {
        public SArrayVector3(IntPtr core) : base(core) { }
        public override void PushBack(Vector3 data)
        {
            SUtility.SetToBufferVector3(data);
            SUtility.ArrayPushBack(m_Core, m_ElementID);
        }

        public override void Set(Vector3 data, int idx)
        {
            SUtility.SetToBufferVector3(data);
            SUtility.ArraySet(m_Core, idx, m_ElementID);
        }

        public override Vector3 Get(int idx)
        {
            SUtility.ArrayGet(m_Core, idx, m_ElementID);
            return SUtility.GetFromBufferVector3();
        }
    }
    /// <summary>
    /// IEntity array
    /// </summary>
    public class SArrayEntity : SArrayClass<IEntity>
    {
        public SArrayEntity(IntPtr core) : base(core) { }
        public override void PushBack(IEntity? data)
        {
            SUtility.SetToBufferEntity(data == null ? IntPtr.Zero : data.Ptr);
            SUtility.ArrayPushBack(m_Core, m_ElementID);
        }

        public override void Set(IEntity? data, int idx)
        {
            SUtility.SetToBufferEntity(data == null ? IntPtr.Zero : data.Ptr);
            SUtility.ArraySet(m_Core, idx, m_ElementID);
        }

        public override IEntity? Get(int idx)
        {
            SUtility.ArrayGet(m_Core, idx, m_ElementID);
            return SPtrMgr.Instance.Get(SUtility.GetFromBufferEntity()) as IEntity;
        }
    }
    /// <summary>
    /// String array
    /// </summary>
    public class SArrayString : SArrayClass<string>
    {
        public SArrayString(IntPtr core) : base(core) { }
        public override void PushBack(string? data)
        {
            SharpHelper.SetToBufferString(data);
            SUtility.ArrayPushBack(m_Core, m_ElementID);
        }

        public override void Set(string? data, int idx)
        {
            SharpHelper.SetToBufferString(data);
            SUtility.ArraySet(m_Core, idx, m_ElementID);
        }

        public override string Get(int idx)
        {
            SUtility.ArrayGet(m_Core, idx, m_ElementID);
            SUtility.GetFromBufferString(SUtility.CharBuffer, SUtility.CharBuffer.Length);
            return SUtility.BuildStringFromCharBuffer();
        }
    }
}
