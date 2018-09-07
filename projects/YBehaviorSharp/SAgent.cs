using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace YBehaviorSharp
{
    public class SEntity : IDisposable
    {
        bool m_bReference = false;

        IntPtr m_Core;
        public IntPtr Core { get { return m_Core; } }

        public SEntity()
        {
            m_Core = SharpHelper.CreateEntity();
        }

        public SEntity(IntPtr core)
        {
            m_Core = core;
            m_bReference = true;
        }

        public void Dispose()
        {
            if (!m_bReference)
                SharpHelper.DeleteEntity(m_Core);
        }
    }

    public class SAgent : IDisposable
    {
        SEntity m_Entity;
        IntPtr m_Core;
        public IntPtr Core { get { return m_Core; } }

        public SAgent(SEntity entity)
        {
            m_Entity = entity;
            m_Core = SharpHelper.CreateAgent(entity.Core);
        }
        public void Dispose()
        {
            SharpHelper.DeleteAgent(m_Core);
        }
    }
}
