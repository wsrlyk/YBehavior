using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace YBehaviorSharp
{
    /// <summary>
    /// This kind of node will share one instance and will not have its own data
    /// </summary>
    public interface IStaticTreeNode
    {
        /// <summary>
        /// Will be called every tick
        /// </summary>
        /// <param name="pNode">Pointer to the node in cpp</param>
        /// <param name="pAgent">Pointer to the agent in cpp</param>
        /// <returns></returns>
        NodeState OnNodeUpdate(IntPtr pNode, IntPtr pAgent);

        string NodeName { get; }
    }

    /// <summary>
    /// Every node will be instanced and keep its own variables
    /// </summary>
    public interface ITreeNode : IStaticTreeNode
    {
        /// <summary>
        /// Will be called only once when loaded
        /// </summary>
        /// <param name="pNode">Pointer to the node in cpp</param>
        /// <param name="pData">Pointer to the config in cpp</param>
        /// <returns></returns>
        bool OnNodeLoaded(IntPtr pNode, IntPtr pData);
    }
    public partial class SharpHelper
    {
        [DllImport(VERSION.dll)]
        static extern void RegisterSharpNode(string name, int index);

        [DllImport(VERSION.dll)]
        public static extern void RegisterSharpNodeCallback(OnNodeLoaded onload, OnNodeUpdate onupdate);

        /// <summary>
        /// Every treenode in C# should be registered by this function at the start of the game
        /// </summary>
        /// <param name="node"></param>
        public static void RegisterTreeNode(ITreeNode node)
        {
            int index = STreeNodeMgr.Instance.Register(node);
            if (index < 0)
                return;
            RegisterSharpNode(node.NodeName, index);
        }
    }

    class STreeNodeMgr
    {
        public static STreeNodeMgr Instance { get; private set; } = new STreeNodeMgr();

        Dictionary<IntPtr, ITreeNode> m_dynamicNodes = new Dictionary<IntPtr, ITreeNode>();
        List<IStaticTreeNode> m_allNodes = new List<IStaticTreeNode>();

        public STreeNodeMgr()
        {
            SharpHelper.RegisterSharpNodeCallback(OnNodeLoaded, OnNodeUpdate);
        }
        public int Register(IStaticTreeNode node)
        {
            if (node == null)
                return -1;
            m_allNodes.Add(node);
            return m_allNodes.Count - 1;
        }

        bool OnNodeLoaded(IntPtr pNode, IntPtr pData, int index)
        {
            if (index < 0 || index >= m_allNodes.Count)
                return false;
            var node = m_allNodes[index];
            if (node == null) return false;
            if (!(node is ITreeNode))
                return true;
            if (!m_dynamicNodes.TryGetValue(pNode, out var dynamicNode))
            {
                dynamicNode = Activator.CreateInstance(node.GetType()) as ITreeNode;
                m_dynamicNodes.Add(pNode, dynamicNode);
            }
            else
            {
                //multi load, should not run to here
            }
            return dynamicNode.OnNodeLoaded(pNode, pData);
        }

        NodeState OnNodeUpdate(IntPtr pNode, IntPtr pAgent, int index)
        {
            if (index < 0 || index >= m_allNodes.Count)
                return NodeState.NS_INVALID;
            var node = m_allNodes[index];
            if (node == null) return NodeState.NS_INVALID;
            if (!(node is ITreeNode))
                return node.OnNodeUpdate(pNode, pAgent);
            if (m_dynamicNodes.TryGetValue(pNode, out var dynamicNode))
            {
                return dynamicNode.OnNodeUpdate(pNode, pAgent);
            }
            //not found, should not run to here
            return NodeState.NS_INVALID;
        }
    }
}
