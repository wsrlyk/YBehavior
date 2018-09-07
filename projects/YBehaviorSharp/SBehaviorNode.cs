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
            m_Target = new SVariableEntity(YBehaviorSharp.SharpHelper.CreateVariable(pNode, "Target", pData, true, YBehaviorSharp.SUtility.POINTER_CHAR));
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
        IntPtr m_Src;
        IntPtr m_Des;

        public SetVector3Action()
        {
            m_OnLoadCallback = new OnNodeLoaded(OnNodeLoaded);
            m_OnUpdateCallback = new OnNodeUpdate(OnNodeUpdate);
            m_Name = "SetVector3Action";
        }

        protected bool OnNodeLoaded(IntPtr pNode, IntPtr pData)
        {
            m_Src = YBehaviorSharp.SharpHelper.CreateVariable(pNode, "Src", pData, true, YBehaviorSharp.SUtility.POINTER_CHAR);
            m_Des = YBehaviorSharp.SharpHelper.CreateVariable(pNode, "Des", pData, true, YBehaviorSharp.SUtility.POINTER_CHAR);

            return true;
        }

        protected NodeState OnNodeUpdate(IntPtr pNode, IntPtr pAgent)
        {
            Console.WriteLine("SetVector3Action Update");

            Vector3 src = YBehaviorSharp.SharpHelper.GetVariableVector3(pAgent, m_Src);
            src.x += 1;
            YBehaviorSharp.SharpHelper.SetVariableVector3(pAgent, m_Des, src);

            return NodeState.NS_SUCCESS;
        }
    }
}
