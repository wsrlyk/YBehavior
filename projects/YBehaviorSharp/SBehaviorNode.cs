using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace YBehaviorSharp
{
    public class SBehaviorNode
    {
        public void Register()
        {
            SharpHelper.RegisterSharpNode(m_Name, m_OnLoadCallback, m_OnUpdateCallback);
        }

        protected OnNodeLoaded m_OnLoadCallback;
        protected OnNodeUpdate m_OnUpdateCallback;
        protected string m_Name;
    }

    public class SelectTargetAction : SBehaviorNode
    {
        SVariableEntity m_Target;

        public SelectTargetAction()
        {
            m_OnLoadCallback = new OnNodeLoaded(OnNodeLoaded);
            m_OnUpdateCallback = new OnNodeUpdate(OnNodeUpdate);
            m_Name = "SelectTargetAction";
        }

        protected bool OnNodeLoaded(IntPtr pNode, IntPtr pData)
        {
            m_Target = new SVariableEntity(YBehaviorSharp.SharpHelper.CreateVariable(pNode, "Target", pData, YBehaviorSharp.SUtility.POINTER_CHAR));
            if (!m_Target.IsValid)
            {
                return false;
            }

            return true;
        }

        protected NodeState OnNodeUpdate(IntPtr pNode, IntPtr pAgent)
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
            m_OnLoadCallback = new OnNodeLoaded(OnNodeLoaded);
            m_OnUpdateCallback = new OnNodeUpdate(OnNodeUpdate);
            m_Name = "GetTargetNameAction";
        }

        protected bool OnNodeLoaded(IntPtr pNode, IntPtr pData)
        {
            return true;
        }

        protected NodeState OnNodeUpdate(IntPtr pNode, IntPtr pAgent)
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
            m_OnLoadCallback = new OnNodeLoaded(OnNodeLoaded);
            m_OnUpdateCallback = new OnNodeUpdate(OnNodeUpdate);
            m_Name = "SetVector3Action";
        }

        protected bool OnNodeLoaded(IntPtr pNode, IntPtr pData)
        {
            m_Src = YBehaviorSharp.SVariableHelper.CreateVariable(pNode, "Src", pData, YBehaviorSharp.SUtility.POINTER_CHAR);
            m_Des = YBehaviorSharp.SVariableHelper.CreateVariable(pNode, "Des", pData, YBehaviorSharp.SUtility.POINTER_CHAR);

            return true;
        }

        protected NodeState OnNodeUpdate(IntPtr pNode, IntPtr pAgent)
        {
            Console.WriteLine("SetVector3Action Update");

            Vector3 src = (m_Src as SVariableVector3).Get(pAgent);
            src.x += 1;
            (m_Des as SVariableVector3).Set(pAgent, src);

            return NodeState.NS_SUCCESS;
        }
    }
}
