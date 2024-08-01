using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Linq;

namespace YBehaviorSharp
{
    using TYPEID = System.Int32;
    using BOOL = System.Byte;

    public interface ISArray
    {
        void Init(IntPtr core);
        void Clear();
        int GetLength();
  }
    abstract public class SArray<T> : ISArray
    {
        protected IntPtr m_Core;
        protected TYPEID m_TypeID;
        public SArray(IntPtr core, TYPEID type)
        {
            m_Core = core;
            m_TypeID = type;
        }
        public void Init(IntPtr core)
        {
            m_Core = core;
        }
        public void Clear()
        {
            SharpHelper.ArrayClear(m_Core, m_TypeID);
        }

        public int GetLength()
        {
            return (int)SharpHelper.ArrayGetSize(m_Core, m_TypeID);
        }

        abstract public void PushBack(T data);
        abstract public void Set(T data, int idx);
        abstract public T Get(int idx);
    }

    public class SArrayHelper
    {
        public static ISArray GetArray(IntPtr ptr, TYPEID elementType)
        {
            var t = s_ArrayTypes[elementType];
            return Activator.CreateInstance(t, ptr) as ISArray;
        }

        static System.Type[] s_ArrayTypes = new Type[7];
        static SArrayHelper()
        {
            s_ArrayTypes[GetClassType<int>.ID] = typeof(SArrayInt);
            s_ArrayTypes[GetClassType<float>.ID] = typeof(SArrayFloat);
            s_ArrayTypes[GetClassType<ulong>.ID] = typeof(SArrayUlong);
            s_ArrayTypes[GetClassType<bool>.ID] = typeof(SArrayBool);
            s_ArrayTypes[GetClassType<Vector3>.ID] = typeof(SArrayVector3);
            s_ArrayTypes[GetClassType<string>.ID] = typeof(SArrayString);
            s_ArrayTypes[GetClassType<IEntity>.ID] = typeof(SArrayEntity);
        }
    }
    ////////////////////////////////////////////////////////////////

    public class SArrayInt : SArray<int>
    {
        public SArrayInt(IntPtr core) : base(core, GetClassType<int>.ID) { }
        public override void PushBack(int data)
        {
            SharpHelper.SetToBufferInt(data);
            SharpHelper.ArrayPushBack(m_Core, m_TypeID);
        }

        public override void Set(int data, int idx)
        {
            SharpHelper.SetToBufferInt(data);
            SharpHelper.ArraySet(m_Core, idx, m_TypeID);
        }

        public override int Get(int idx)
        {
            SharpHelper.ArrayGet(m_Core, idx, m_TypeID);
            return SharpHelper.GetFromBufferInt();
        }
    }

    public class SArrayFloat : SArray<float>
    {
        public SArrayFloat(IntPtr core) : base(core, GetClassType<float>.ID) { }
        public override void PushBack(float data)
        {
            SharpHelper.SetToBufferFloat(data);
            SharpHelper.ArrayPushBack(m_Core, m_TypeID);
        }

        public override void Set(float data, int idx)
        {
            SharpHelper.SetToBufferFloat(data);
            SharpHelper.ArraySet(m_Core, idx, m_TypeID);
        }

        public override float Get(int idx)
        {
            SharpHelper.ArrayGet(m_Core, idx, m_TypeID);
            return SharpHelper.GetFromBufferFloat();
        }
    }

    public class SArrayUlong : SArray<ulong>
    {
        public SArrayUlong(IntPtr core) : base(core, GetClassType<ulong>.ID) { }
        public override void PushBack(ulong data)
        {
            SharpHelper.SetToBufferUlong(data);
            SharpHelper.ArrayPushBack(m_Core, m_TypeID);
        }

        public override void Set(ulong data, int idx)
        {
            SharpHelper.SetToBufferUlong(data);
            SharpHelper.ArraySet(m_Core, idx, m_TypeID);
        }

        public override ulong Get(int idx)
        {
            SharpHelper.ArrayGet(m_Core, idx, m_TypeID);
            return SharpHelper.GetFromBufferUlong();
        }
    }

    public class SArrayBool : SArray<bool>
    {
        public SArrayBool(IntPtr core) : base(core, GetClassType<bool>.ID) { }
        public override void PushBack(bool data)
        {
            SharpHelper.SetToBufferBool(SharpHelper.ConvertBool(data));
            SharpHelper.ArrayPushBack(m_Core, m_TypeID);
        }

        public override void Set(bool data, int idx)
        {
            SharpHelper.SetToBufferBool(SharpHelper.ConvertBool(data));
            SharpHelper.ArraySet(m_Core, idx, m_TypeID);
        }

        public override bool Get(int idx)
        {
            SharpHelper.ArrayGet(m_Core, idx, m_TypeID);
            return SharpHelper.ConvertBool(SharpHelper.GetFromBufferBool());
        }
    }

    public class SArrayVector3 : SArray<Vector3>
    {
        public SArrayVector3(IntPtr core) : base(core, GetClassType<Vector3>.ID) { }
        public override void PushBack(Vector3 data)
        {
            SharpHelper.SetToBufferVector3(data);
            SharpHelper.ArrayPushBack(m_Core, m_TypeID);
        }

        public override void Set(Vector3 data, int idx)
        {
            SharpHelper.SetToBufferVector3(data);
            SharpHelper.ArraySet(m_Core, idx, m_TypeID);
        }

        public override Vector3 Get(int idx)
        {
            SharpHelper.ArrayGet(m_Core, idx, m_TypeID);
            return SharpHelper.GetFromBufferVector3();
        }
    }

    public class SArrayEntity : SArray<IEntity>
    {
        public SArrayEntity(IntPtr core) : base(core, GetClassType<IEntity>.ID) { }
        public override void PushBack(IEntity data)
        {
            SharpHelper.SetToBufferEntity(data.Ptr);
            SharpHelper.ArrayPushBack(m_Core, m_TypeID);
        }

        public override void Set(IEntity data, int idx)
        {
            SharpHelper.SetToBufferEntity(data.Ptr);
            SharpHelper.ArraySet(m_Core, idx, m_TypeID);
        }

        public override IEntity Get(int idx)
        {
            SharpHelper.ArrayGet(m_Core, idx, m_TypeID);
            return SPtrMgr.Instance.Get(SharpHelper.GetFromBufferEntity()) as IEntity;
        }
    }

    public class SArrayString : SArray<string>
    {
        public SArrayString(IntPtr core) : base(core, GetClassType<string>.ID) { }
        public override void PushBack(string data)
        {
            SharpHelper.SetToBufferString(data);
            SharpHelper.ArrayPushBack(m_Core, m_TypeID);
        }

        public override void Set(string data, int idx)
        {
            SharpHelper.SetToBufferString(data);
            SharpHelper.ArraySet(m_Core, idx, m_TypeID);
        }

        public override string Get(int idx)
        {
            SharpHelper.ArrayGet(m_Core, idx, m_TypeID);
            SharpHelper.GetFromBufferString(SUtility.CharBuffer, SUtility.CharBuffer.Length);
            return SUtility.BuildStringFromCharBuffer();
        }
    }
}
