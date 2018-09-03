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
        public SelectTargetAction()
        {
            m_OnLoadCallback = new OnNodeLoaded(OnNodeLoaded);
            m_OnUpdateCallback = new OnNodeUpdate(OnNodeUpdate);
            m_Name = "SelectTargetAction";
        }

        protected bool OnNodeLoaded(IntPtr pNode, IntPtr pData)
        {
            return true;
        }

        protected NodeState OnNodeUpdate(IntPtr pNode, IntPtr pData)
        {
            Console.WriteLine("SelectTargetAction Update");
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

        protected NodeState OnNodeUpdate(IntPtr pNode, IntPtr pData)
        {
            Console.WriteLine("GetTargetNameAction Update");
            return NodeState.NS_SUCCESS;
        }
    }
}
