using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace YBehaviorSharp
{
    public interface IPtr
    {
        IntPtr Ptr { get; }
    }

    public class SPtr : IDisposable, IPtr
    {
        protected IntPtr m_Ptr;
        public IntPtr Ptr { get { return m_Ptr; } }
        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void OnDispose(bool disposing) { }

        protected void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                }

                if (m_Ptr != IntPtr.Zero)
                {
                    SPtrMgr.Instance.Remove(this);
                    OnDispose(disposing);
                    m_Ptr = IntPtr.Zero;
                }

                disposedValue = true;
            }
        }

        ~SPtr()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }
        #endregion

    }
    public class SEntity : SPtr
    {
        public IntPtr Core { get { return m_Ptr; } }

        public SEntity()
        {
            m_Ptr = SharpHelper.CreateEntity();
            SPtrMgr.Instance.Add(this);
        }

        protected override void OnDispose(bool disposing)
        {
            SharpHelper.DeleteEntity(m_Ptr);
        }
    }

    public class SAgent : SPtr
    {
        SEntity m_Entity;
        public SEntity Entity { get { return m_Entity; } }

        public IntPtr Core { get { return m_Ptr; } }

        public SAgent(SEntity entity)
        {
            m_Entity = entity;
            m_Ptr = SharpHelper.CreateAgent(m_Entity.Core);
            SPtrMgr.Instance.Add(this);
        }
        protected override void OnDispose(bool disposing)
        {
            SharpHelper.DeleteAgent(m_Ptr);
        }
    }

    public class SPtrMgr
    {
        static SPtrMgr s_Mgr = new SPtrMgr();
        public static SPtrMgr Instance { get { return s_Mgr; } }

        Dictionary<IntPtr, IPtr> m_Dic = new Dictionary<IntPtr, IPtr>();

        public void Add(IPtr entity)
        {
            if (entity == null)
                return;

            m_Dic.Add(entity.Ptr, entity);
        }

        public void Remove(IPtr entity)
        {
            if (entity == null)
                return;

            m_Dic.Remove(entity.Ptr);
        }

        public IPtr Get(IntPtr core)
        {
            if (m_Dic.TryGetValue(core, out IPtr entity))
                return entity;
            return null;
        }
    }
}
