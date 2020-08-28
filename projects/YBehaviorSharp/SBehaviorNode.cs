using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace YBehaviorSharp
{
    public abstract class SBehaviorNode
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
        public SBehaviorNode()
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

        protected bool HasLogPoint()
        {
            return SharpHelper.HasLogPoint(m_pNode);
        }

        protected void LogSharedData(SVariable v, bool before)
        {
            SharpHelper.LogSharedData(m_pNode, v.Core, before);
        }

        protected void LogDebugInfo(string s)
        {
            SharpHelper.LogDebugInfo(m_pNode, s);
        }

        abstract protected bool OnNodeLoaded(IntPtr pNode, IntPtr pData);

        abstract protected NodeState OnNodeUpdate(IntPtr pNode, IntPtr pAgent);

        protected string m_Name;
        protected IntPtr m_pNode;
    }

    public class SelectTargetAction : SBehaviorNode
    {
        SVariableEntity m_Target;

        public SelectTargetAction()
        {
            m_Name = "SelectTargetAction";
        }

        protected override bool OnNodeLoaded(IntPtr pNode, IntPtr pData)
        {
            m_Target = new SVariableEntity(YBehaviorSharp.SharpHelper.CreateVariable(pNode, "Target", pData, YBehaviorSharp.SUtility.POINTER_CHAR));
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

    public class GetTargetNameAction : SBehaviorNode
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

    public class SetVector3Action : SBehaviorNode
    {
        SVariable m_Src;
        SVariable m_Des;

        public SetVector3Action()
        {
            m_Name = "SetVector3Action";
        }

        protected override bool OnNodeLoaded(IntPtr pNode, IntPtr pData)
        {
            m_Src = YBehaviorSharp.SVariableHelper.CreateVariable(pNode, "Src", pData, YBehaviorSharp.SUtility.POINTER_CHAR);
            m_Des = YBehaviorSharp.SVariableHelper.CreateVariable(pNode, "Des", pData, YBehaviorSharp.SUtility.POINTER_CHAR);

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
