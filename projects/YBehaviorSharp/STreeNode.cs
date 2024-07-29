using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace YBehaviorSharp
{
    public abstract class STreeNode
    {
        public void Register()
        {
            OnNodeLoaded onload = NodeLoaded;
            OnNodeUpdate onupdate = NodeUpdate;
            s_OnLoadCallback.Add(onload);
            s_OnUpdateCallback.Add(onupdate);
            SharpHelper.RegisterSharpNode(m_Name, onload, onupdate);
        }

        static List<OnNodeLoaded> s_OnLoadCallback = new List<OnNodeLoaded>();
        static List<OnNodeUpdate> s_OnUpdateCallback = new List<OnNodeUpdate>();
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

    public class SelectTargetAction : STreeNode
    {
        SVariableEntity m_Target;

        public SelectTargetAction()
        {
            m_Name = "SelectTargetAction";
        }

        protected override bool OnNodeLoaded(IntPtr pNode, IntPtr pData)
        {
            m_Target = new SVariableEntity(YBehaviorSharp.SharpHelper.CreateVariable(pNode, "Target", pData, true));
            if (!m_Target.IsValid)
            {
                return false;
            }

            return true;
        }

        protected override NodeState OnNodeUpdate(IntPtr pNode, IntPtr pAgent)
        {
            Console.WriteLine("SelectTargetAction Update");
            SEntity entity = m_Target.Get(pAgent);
            return NodeState.NS_SUCCESS;
        }
    }

    public class GetTargetNameAction : STreeNode
    {
        public GetTargetNameAction()
        {
            m_Name = "GetTargetNameAction";
        }

        protected override bool OnNodeLoaded(IntPtr pNode, IntPtr pData)
        {
            return true;
        }

        protected override NodeState OnNodeUpdate(IntPtr pNode, IntPtr pAgent)
        {
            Console.WriteLine("GetTargetNameAction Update");
            return NodeState.NS_SUCCESS;
        }
    }

    public class SetVector3Action : STreeNode
    {
        SVariable m_Src;
        SVariable m_Des;

        public SetVector3Action()
        {
            m_Name = "SetVector3Action";
        }

        protected override bool OnNodeLoaded(IntPtr pNode, IntPtr pData)
        {
            m_Src = YBehaviorSharp.SVariableHelper.CreateVariable(pNode, "Src", pData, true);
            m_Des = YBehaviorSharp.SVariableHelper.CreateVariable(pNode, "Des", pData, true);

            return true;
        }

        protected override NodeState OnNodeUpdate(IntPtr pNode, IntPtr pAgent)
        {
            Console.WriteLine("SetVector3Action Update");

            Vector3 src = (m_Src as SVariableVector3).Get(pAgent);
            src.x += 1;
            (m_Des as SVariableVector3).Set(pAgent, src);

            return NodeState.NS_SUCCESS;
        }
    }
}
