﻿using System;
using System.Collections.Generic;
using System.Text;

namespace YBehaviorSharp
{
    using TYPEID = System.Int32;
    using INT = System.Int32;
    using BOOL = System.Byte;


    public class SVariableHelper
    {
        public static SVariable CreateVariable(IntPtr pNode, string attrName, IntPtr data, bool noConst = false)
        {
            IntPtr v = SharpHelper.CreateVariable(pNode, attrName, data, noConst);
            if (v == IntPtr.Zero)
                return null;
            return GetVaraible(v);
        }
        private static SVariable GetVaraible(IntPtr ptr)
        {
            var type = SharpHelper.GetVariableTypeID(ptr);
            var elementtype = SharpHelper.GetVariableElementTypeID(ptr);
            if (type == elementtype)
            {
                var t = s_VariableTypes[type];
                return Activator.CreateInstance(t, ptr) as SVariable;
            }
            return new SArrayVaraible(ptr);
        }

        static System.Type[] s_VariableTypes = new Type[7];
        static SVariableHelper()
        {
            s_VariableTypes[GetClassType<int>.ID] = typeof(SVariableInt);
            s_VariableTypes[GetClassType<float>.ID] = typeof(SVariableFloat);
            s_VariableTypes[GetClassType<ulong>.ID] = typeof(SVariableUlong);
            s_VariableTypes[GetClassType<bool>.ID] = typeof(SVariableBool);
            s_VariableTypes[GetClassType<Vector3>.ID] = typeof(SVariableVector3);
            s_VariableTypes[GetClassType<string>.ID] = typeof(SVariableString);
            s_VariableTypes[GetClassType<IEntity>.ID] = typeof(SVariableEntity);
        }
    }

    public class SVariable
    {
        public IntPtr Ptr { get; protected set; } = IntPtr.Zero;

        protected TYPEID m_TypeID;

        public bool IsValid { get { return Ptr != IntPtr.Zero; } }

        public SVariable(IntPtr ptr)
        {
            Ptr = ptr;
            m_TypeID = SharpHelper.GetVariableTypeID(ptr);
        }
    }

    public class SArrayVaraible : SVariable
    {
        protected TYPEID m_ElementTypeID;
        protected ISArray m_Array;

        public SArrayVaraible(IntPtr core) : base(core)
        {
            m_ElementTypeID = SharpHelper.GetVariableElementTypeID(core);
            m_Array = SArrayHelper.GetArray(IntPtr.Zero, m_ElementTypeID);
        }

        public ISArray Get(IntPtr pAgent)
        {
            IntPtr ptr = SharpHelper.GetVariableValuePtr(pAgent, Ptr);
            m_Array.Init(ptr);
            return m_Array;
        }

        ///> No Set. Just Get and operate it.
        //public void Set(IntPtr pAgent, ISArray data)
        //{

        //}
    }

    public abstract class SVariable<T> : SVariable
    {
        public SVariable(IntPtr core)
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

    public class SVariableEntity : SVariable<IEntity>
    {
        public SVariableEntity(IntPtr core) : base(core) { }

        public override IEntity Get(IntPtr pAgent)
        {
            if (SharpHelper.GetVariableValue(pAgent, Ptr))
            {
                return SPtrMgr.Instance.Get(SharpHelper.GetFromBufferEntity()) as IEntity;
            }
            return null;
        }

        public override void Set(IntPtr pAgent, IEntity data)
        {
            SharpHelper.SetToBufferEntity(data.Ptr);
            SharpHelper.SetVariableValue(pAgent, Ptr);
        }
    }

    public class SVariableInt : SVariable<int>
    {
        public SVariableInt(IntPtr core) : base(core) { }

        public override int Get(IntPtr pAgent)
        {
            if (SharpHelper.GetVariableValue(pAgent, Ptr))
            {
                return SharpHelper.GetFromBufferInt();
            }
            return 0;
        }

        public override void Set(IntPtr pAgent, int data)
        {
            SharpHelper.SetToBufferInt(data);
            SharpHelper.SetVariableValue(pAgent, Ptr);
        }
    }

    public class SVariableFloat : SVariable<float>
    {
        public SVariableFloat(IntPtr core) : base(core) { }

        public override float Get(IntPtr pAgent)
        {
            if (SharpHelper.GetVariableValue(pAgent, Ptr))
            {
                return SharpHelper.GetFromBufferFloat();
            }
            return 0;
        }

        public override void Set(IntPtr pAgent, float data)
        {
            SharpHelper.SetToBufferFloat(data);
            SharpHelper.SetVariableValue(pAgent, Ptr);
        }
    }

    public class SVariableUlong : SVariable<ulong>
    {
        public SVariableUlong(IntPtr core) : base(core) { }

        public override ulong Get(IntPtr pAgent)
        {
            if (SharpHelper.GetVariableValue(pAgent, Ptr))
            {
                return SharpHelper.GetFromBufferUlong();
            }
            return 0;
        }

        public override void Set(IntPtr pAgent, ulong data)
        {
            SharpHelper.SetToBufferUlong(data);
            SharpHelper.SetVariableValue(pAgent, Ptr);
        }
    }

    public class SVariableBool : SVariable<bool>
    {
        public SVariableBool(IntPtr core) : base(core) { }

        public override bool Get(IntPtr pAgent)
        {
            if (SharpHelper.GetVariableValue(pAgent, Ptr))
            {
                return SharpHelper.ConvertBool(SharpHelper.GetFromBufferBool());
            }
            return false;
        }

        public override void Set(IntPtr pAgent, bool data)
        {
            SharpHelper.SetToBufferBool(SharpHelper.ConvertBool(data));
            SharpHelper.SetVariableValue(pAgent, Ptr);
        }
    }

    public class SVariableVector3 : SVariable<Vector3>
    {
        public SVariableVector3(IntPtr core) : base(core) { }

        public override Vector3 Get(IntPtr pAgent)
        {
            if (SharpHelper.GetVariableValue(pAgent, Ptr))
            {
                return SharpHelper.GetFromBufferVector3();
            }
            return Vector3.zero;
        }

        public override void Set(IntPtr pAgent, Vector3 data)
        {
            SharpHelper.SetToBufferVector3(data);
            SharpHelper.SetVariableValue(pAgent, Ptr);
        }
    }

    public class SVariableString : SVariable<string>
    {
        public SVariableString(IntPtr core) : base(core) { }

        public override string Get(IntPtr pAgent)
        {
            if (SharpHelper.GetVariableValue(pAgent, Ptr))
            {
                return SharpHelper.GetFromBufferString();
            }
            return string.Empty;
        }

        public override void Set(IntPtr pAgent, string data)
        {
            SharpHelper.SetToBufferString(data);
            SharpHelper.SetVariableValue(pAgent, Ptr);
        }
    }
}
