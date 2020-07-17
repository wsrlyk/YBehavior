using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace YBehavior.Editor.Core.New
{
    public enum GraphType
    {
        TREE,
        FSM,
    }

    public class Graph
    {
        public static readonly int FLAG_LOADING = 1 << 0;

        protected List<NodeBase> m_NodeList = new List<NodeBase>();

        public virtual void RefreshNodeUID(uint startUID) { }
        int m_State = 0;
        public void SetFlag(int flag)
        {
            m_State |= flag;
        }
        public void RemoveFlag(int flag)
        {
            m_State &= (~flag);
        }
        public bool IsInState(int flag)
        {
            return (m_State & flag) != 0;
        }
    }

    public class Tree : Graph, IVariableCollectionOwner
    {
        RootTreeNode m_Root;
        public RootTreeNode Root { get { return m_Root; } }

        protected TreeMemory m_TreeMemory;
        public TreeMemory TreeMemory { get { return m_TreeMemory; } }
        protected InOutMemory m_InOutMemory;
        public InOutMemory InOutMemory { get { return m_InOutMemory; } }

        public TreeMemory SharedData { get { return m_TreeMemory; } }
        public InOutMemory InOutData { get { return m_InOutMemory; } }

        public Tree()
        {
            CreateRoot();
        }

        public void CreateRoot()
        {
            m_Root = TreeNodeMgr.Instance.CreateNodeByName("Root") as RootTreeNode;
            m_TreeMemory = m_Root.Variables as TreeMemory;
            m_InOutMemory = new InOutMemory(this, true);
        }

        public override void RefreshNodeUID(uint startUID)
        {
            if (IsInState(FLAG_LOADING))
                return;
            RefreshNodeUIDFromRoot(Root, startUID);
        }

        public void RefreshNodeUIDFromRoot(NodeBase node, uint startUID)
        {
            if (IsInState(FLAG_LOADING))
                return;
            uint uid = startUID;
            _RefreshNodeUID(node, ref uid);
        }

        public void RefreshNodeUIDFromMiddle(NodeBase node)
        {
            if (IsInState(FLAG_LOADING))
                return;
            uint uid = node.UID - 1;
            _RefreshNodeUID(node, ref uid);
        }

        void _RefreshNodeUID(NodeBase node, ref uint uid)
        {
            if (node.Disabled)
                node.UID = 0;
            else
                node.UID = ++uid;

            foreach (NodeBase chi in node.Conns)
            {
                _RefreshNodeUID(chi, ref uid);
            }
        }

        public void OnVariableValueChanged(Variable v)
        {
            ///> Nothing to do
        }
    }
}
