using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace YBehavior.Editor.Core.New
{
    public class Graph
    {
        protected List<NodeBase> m_NodeList = new List<NodeBase>();

        public virtual void RefreshNodeUID() { }

    }

    public class Tree : Graph, IVariableDataSource
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
        }

        public override void RefreshNodeUID()
        {
            RefreshNodeUIDFromRoot(Root);
        }

        public void RefreshNodeUIDFromRoot(TreeNode node)
        {
            uint uid = 0;
            _RefreshNodeUID(node, ref uid);
        }

        public void RefreshNodeUIDFromMiddle(TreeNode node)
        {
            uint uid = node.UID - 1;
            _RefreshNodeUID(node, ref uid);
        }

        void _RefreshNodeUID(TreeNode node, ref uint uid)
        {
            if (node.Disabled)
                node.UID = 0;
            else
                node.UID = ++uid;

            foreach (TreeNode chi in node.Conns)
            {
                _RefreshNodeUID(chi, ref uid);
            }
        }

    }
}
