using System;

namespace YBehaviorSharp
{
    public abstract class STreeNode
    {
        public void Register()
        {
            OnNodeLoaded onload = NodeLoaded;
            OnNodeUpdate onupdate = NodeUpdate;

            SharpHelper.RegisterSharpNode(m_Name, onload, onupdate);
        }

        public STreeNode()
        {
        }

        protected bool NodeLoaded(IntPtr pNode, IntPtr pData)
        {
            m_pNode = pNode;
            return OnNodeLoaded(pNode, pData);
        }
        protected NodeState NodeUpdate(IntPtr pNode, IntPtr pAgent)
        {
            return OnNodeUpdate(pNode, pAgent);
        }

        protected bool HasDebugPoint()
        {
            return SharpHelper.HasDebugPoint(m_pNode);
        }

        protected void LogVariable(SVariable v, bool before)
        {
            if (v == null)
                return;
            SharpHelper.LogVariable(m_pNode, v.Core, before);
        }

        protected void LogInfo(string s)
        {
            SharpHelper.LogInfo(m_pNode, s);
        }

        abstract protected bool OnNodeLoaded(IntPtr pNode, IntPtr pData);

        abstract protected NodeState OnNodeUpdate(IntPtr pNode, IntPtr pAgent);

        protected string m_Name;
        protected IntPtr m_pNode;
    }
}
