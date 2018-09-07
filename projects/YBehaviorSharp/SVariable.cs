using System;
using System.Collections.Generic;
using System.Text;

namespace YBehaviorSharp
{
    using TYPEID = System.Int32;
    using INT = System.Int32;
    using BOOL = System.Int16;

    class SVariable<T>
    {
        protected IntPtr m_Core;

        public bool IsValid { get { return m_Core != IntPtr.Zero; } }

        public SVariable(IntPtr core)
        {
            if (core != IntPtr.Zero && GetClassType<T>.ID == SharpHelper.GetVariableTypeID(core))
            {
                m_Core = core;
            }
            else
            {
                m_Core = IntPtr.Zero;
            }
        }
    }

    class SVariableEntity : SVariable<EntityWrapper>
    {
        public SVariableEntity(IntPtr core) : base(core)
        {
        }

        public SEntity Get(IntPtr pAgent)
        {
            return new SEntity(YBehaviorSharp.SharpHelper.GetVariableValue(pAgent, m_Core));
        }

        public void Set(IntPtr pAgent, SEntity entity)
        {
            YBehaviorSharp.SharpHelper.SetVariableValue(pAgent, m_Core, entity.Core);
        }
    }

    class SVariableInt : SVariable<INT>
    {
        public SVariableInt(IntPtr core) : base(core)
        {
        }

        public INT Get(IntPtr pAgent)
        {
            return YBehaviorSharp.SharpHelper.GetVariableInt(pAgent, m_Core);
        }

        public void Set(IntPtr pAgent, INT value)
        {
            YBehaviorSharp.SharpHelper.SetVariableInt(pAgent, m_Core, value);
        }
    }

    class SVariableBool : SVariable<BOOL>
    {
        public SVariableBool(IntPtr core) : base(core)
        {
        }

        public bool Get(IntPtr pAgent)
        {
            return YBehaviorSharp.SharpHelper.GetVariableBool(pAgent, m_Core) != 0;
        }

        public void Set(IntPtr pAgent, bool value)
        {
            YBehaviorSharp.SharpHelper.SetVariableBool(pAgent, m_Core, (BOOL)(value ? 1 : 0));
        }
    }

    class SVariableVector3 : SVariable<Vector3>
    {
        public SVariableVector3(IntPtr core) : base(core)
        {
        }

        public Vector3 Get(IntPtr pAgent)
        {
            return YBehaviorSharp.SharpHelper.GetVariableVector3(pAgent, m_Core);
        }

        public void Set(IntPtr pAgent, Vector3 value)
        {
            YBehaviorSharp.SharpHelper.SetVariableVector3(pAgent, m_Core, value);
        }
    }
}
