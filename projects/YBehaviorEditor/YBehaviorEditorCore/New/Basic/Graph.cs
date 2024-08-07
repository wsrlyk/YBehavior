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
    /// <summary>
    /// Base class of tree/fsm
    /// </summary>
    public class Graph
    {
        /// <summary>
        /// Set this flag when it's loading from file.
        /// When in this state, some events will not be invoked.
        /// </summary>
        public static readonly int FLAG_LOADING = 1 << 0;

        //protected List<NodeBase> m_NodeList = new List<NodeBase>();
        /// <summary>
        /// Refresh the UIDs of nodes
        /// </summary>
        /// <param name="startUID"></param>
        public virtual void RefreshNodeUID(uint startUID) { }
        int m_State = 0;
        /// <summary>
        /// Add state
        /// </summary>
        /// <param name="flag"></param>
        public void SetFlag(int flag)
        {
            m_State |= flag;
        }
        /// <summary>
        /// Remove state
        /// </summary>
        /// <param name="flag"></param>
        public void RemoveFlag(int flag)
        {
            m_State &= (~flag);
        }
        /// <summary>
        /// Check if it's in a state
        /// </summary>
        /// <param name="flag"></param>
        /// <returns></returns>
        public bool IsInState(int flag)
        {
            return (m_State & flag) != 0;
        }
    }
    /// <summary>
    /// Tree data
    /// </summary>
    public class Tree : Graph, IVariableCollectionOwner
    {
        RootTreeNode m_Root;
        /// <summary>
        /// Root node
        /// </summary>
        public RootTreeNode Root { get { return m_Root; } }

        protected TreeMemory m_TreeMemory;
        /// <summary>
        /// Shared and local variables
        /// </summary>
        public TreeMemory TreeMemory { get { return m_TreeMemory; } }
        protected InOutMemory m_InOutMemory;
        /// <summary>
        /// Input and output pins for subtree
        /// </summary>
        public InOutMemory InOutMemory { get { return m_InOutMemory; } }
        /// <summary>
        /// Shared and local variables
        /// </summary>
        public TreeMemory SharedData { get { return m_TreeMemory; } }
        /// <summary>
        /// Input and output pins for subtree
        /// </summary>
        public InOutMemory InOutData { get { return m_InOutMemory; } }

        public Tree()
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
        /// <summary>
        /// Refresh children nodes UID based on a given UID
        /// </summary>
        /// <param name="node">root node</param>
        /// <param name="startUID"></param>
        public void RefreshNodeUIDFromRoot(NodeBase node, uint startUID)
        {
            if (IsInState(FLAG_LOADING))
                return;
            uint uid = startUID;
            _RefreshNodeUID(node, ref uid);
        }
        /// <summary>
        /// Refresh children nodes UID based on root
        /// </summary>
        /// <param name="node">root node</param>
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
        public void OnVariableVBTypeChanged(Variable v)
        {
            ///> Nothing to do
        }
        public void OnVariableETypeChanged(Variable v)
        {
            ///> Nothing to do
        }
    }
}
