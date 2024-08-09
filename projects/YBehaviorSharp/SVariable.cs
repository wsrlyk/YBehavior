using System;
using System.Collections.Generic;
using System.Text;

namespace YBehaviorSharp
{
    using TYPEID = System.Int32;
    using INT = System.Int32;
    using BOOL = System.Byte;


    public class SPinHelper
    {
        public static SPin CreatePin(IntPtr pNode, string attrName, IntPtr data, bool noConst = false)
        {
            IntPtr v = SharpHelper.CreatePin(pNode, attrName, data, noConst);
            if (v == IntPtr.Zero)
                return null;
            return GetPin(v);
        }
        private static SPin GetPin(IntPtr ptr)
        {
            var type = SharpHelper.GetPinTypeID(ptr);
            var elementtype = SharpHelper.GetPinElementTypeID(ptr);
            if (type == elementtype)
            {
                var t = s_PinTypes[type];
                return Activator.CreateInstance(t, ptr) as SPin;
            }
            return new SArrayPin(ptr);
        }

        static System.Type[] s_PinTypes = new Type[7];
        static SPinHelper()
        {
            s_PinTypes[GetClassType<int>.ID] = typeof(SPinInt);
            s_PinTypes[GetClassType<float>.ID] = typeof(SPinFloat);
            s_PinTypes[GetClassType<ulong>.ID] = typeof(SPinUlong);
            s_PinTypes[GetClassType<bool>.ID] = typeof(SPinBool);
            s_PinTypes[GetClassType<Vector3>.ID] = typeof(SPinVector3);
            s_PinTypes[GetClassType<string>.ID] = typeof(SPinString);
            s_PinTypes[GetClassType<IEntity>.ID] = typeof(SPinEntity);
        }
    }

    public class SPin
    {
        public IntPtr Ptr { get; protected set; } = IntPtr.Zero;

        protected TYPEID m_TypeID;

        public bool IsValid { get { return Ptr != IntPtr.Zero; } }

        public SPin(IntPtr ptr)
        {
            Ptr = ptr;
            m_TypeID = SharpHelper.GetPinTypeID(ptr);
        }
    }

    public class SArrayPin : SPin
    {
        protected TYPEID m_ElementTypeID;
        protected ISArray m_Array;

        public SArrayPin(IntPtr core) : base(core)
        {
            m_ElementTypeID = SharpHelper.GetPinElementTypeID(core);
            m_Array = SArrayHelper.GetArray(IntPtr.Zero, m_ElementTypeID);
        }

        public ISArray Get(IntPtr pAgent)
        {
            IntPtr ptr = SharpHelper.GetPinValuePtr(pAgent, Ptr);
            m_Array.Init(ptr);
            return m_Array;
        }

        ///> No Set. Just Get and operate it.
        //public void Set(IntPtr pAgent, ISArray data)
        //{

        //}
    }

    public abstract class SPin<T> : SPin
    {
        public SPin(IntPtr core)
            : base(core)
        {
            if (core != IntPtr.Zero && GetClassType<T>.ID != m_TypeID)
            {
                Ptr = IntPtr.Zero;
            }
        }

        abstract public T Get(IntPtr pAgent);
        abstract public void Set(IntPtr pAgent, T data);
    }

    ////////////////////////////////////////////////////////////////

    public class SPinEntity : SPin<IEntity>
    {
        public SPinEntity(IntPtr core) : base(core) { }

        public override IEntity Get(IntPtr pAgent)
        {
            if (SharpHelper.GetPinValue(pAgent, Ptr))
            {
                return SPtrMgr.Instance.Get(SharpHelper.GetFromBufferEntity()) as IEntity;
            }
            return null;
        }

        public override void Set(IntPtr pAgent, IEntity data)
        {
            SharpHelper.SetToBufferEntity(data.Ptr);
            SharpHelper.SetPinValue(pAgent, Ptr);
        }
    }

    public class SPinInt : SPin<int>
    {
        public SPinInt(IntPtr core) : base(core) { }

        public override int Get(IntPtr pAgent)
        {
            if (SharpHelper.GetPinValue(pAgent, Ptr))
            {
                return SharpHelper.GetFromBufferInt();
            }
            return 0;
        }

        public override void Set(IntPtr pAgent, int data)
        {
            SharpHelper.SetToBufferInt(data);
            SharpHelper.SetPinValue(pAgent, Ptr);
        }
    }

    public class SPinFloat : SPin<float>
    {
        public SPinFloat(IntPtr core) : base(core) { }

        public override float Get(IntPtr pAgent)
        {
            if (SharpHelper.GetPinValue(pAgent, Ptr))
            {
                return SharpHelper.GetFromBufferFloat();
            }
            return 0;
        }

        public override void Set(IntPtr pAgent, float data)
        {
            SharpHelper.SetToBufferFloat(data);
            SharpHelper.SetPinValue(pAgent, Ptr);
        }
    }

    public class SPinUlong : SPin<ulong>
    {
        public SPinUlong(IntPtr core) : base(core) { }

        public override ulong Get(IntPtr pAgent)
        {
            if (SharpHelper.GetPinValue(pAgent, Ptr))
            {
                return SharpHelper.GetFromBufferUlong();
            }
            return 0;
        }

        public override void Set(IntPtr pAgent, ulong data)
        {
            SharpHelper.SetToBufferUlong(data);
            SharpHelper.SetPinValue(pAgent, Ptr);
        }
    }

    public class SPinBool : SPin<bool>
    {
        public SPinBool(IntPtr core) : base(core) { }

        public override bool Get(IntPtr pAgent)
        {
            if (SharpHelper.GetPinValue(pAgent, Ptr))
            {
                return SharpHelper.ConvertBool(SharpHelper.GetFromBufferBool());
            }
            return false;
        }

        public override void Set(IntPtr pAgent, bool data)
        {
            SharpHelper.SetToBufferBool(SharpHelper.ConvertBool(data));
            SharpHelper.SetPinValue(pAgent, Ptr);
        }
    }

    public class SPinVector3 : SPin<Vector3>
    {
        public SPinVector3(IntPtr core) : base(core) { }

        public override Vector3 Get(IntPtr pAgent)
        {
            if (SharpHelper.GetPinValue(pAgent, Ptr))
            {
                return SharpHelper.GetFromBufferVector3();
            }
            return Vector3.zero;
        }

        public override void Set(IntPtr pAgent, Vector3 data)
        {
            SharpHelper.SetToBufferVector3(data);
            SharpHelper.SetPinValue(pAgent, Ptr);
        }
    }

    public class SPinString : SPin<string>
    {
        public SPinString(IntPtr core) : base(core) { }

        public override string Get(IntPtr pAgent)
        {
            if (SharpHelper.GetPinValue(pAgent, Ptr))
            {
                return SharpHelper.GetFromBufferString();
            }
            return string.Empty;
        }

        public override void Set(IntPtr pAgent, string data)
        {
            SharpHelper.SetToBufferString(data);
            SharpHelper.SetPinValue(pAgent, Ptr);
        }
    }
}
