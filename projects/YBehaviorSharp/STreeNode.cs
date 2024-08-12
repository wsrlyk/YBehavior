using YBehaviorSharp;

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace YBehaviorSharp
{
    /// <summary>
    /// To recode running data for a treenode running for multiple frames
    /// </summary>
    public interface ITreeNodeContext
    {
        void OnInit();
        NodeState OnNodeUpdate(IntPtr pNode, IntPtr pAgent, NodeState lastState);
    }

    /// <summary>
    /// A treenode may keep running for multiple frames
    /// </summary>
    public interface IHasTreeNodeContext
    {
        ITreeNodeContext CreateContext();
        void DestroyContext(ITreeNodeContext context);

    }
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
        /// <summary>
        /// Name of node
        /// </summary>
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
        static extern void RegisterSharpNode(string name, int index, bool hasContext);

        [DllImport(VERSION.dll)]
        public static extern void RegisterSharpNodeCallback(
            OnNodeLoaded onNodeLoaded,
            OnNodeUpdate onNodeUpdate, 
            OnNodeContextInit onContextInit,
            OnNodeContextUpdate onContextUpdate);

        /// <summary>
        /// Every treenode in C# should be registered by this function at the start of the game
        /// </summary>
        /// <param name="node"></param>
        public static void RegisterTreeNode(ITreeNode node)
        {
            int index = STreeNodeMgr.Instance.Register(node);
            if (index < 0)
                return;
            RegisterSharpNode(node.NodeName, index, node is IHasTreeNodeContext);
        }
    }

    class STreeNodeMgr
    {
        public static STreeNodeMgr Instance { get; private set; } = new STreeNodeMgr();

        Dictionary<IntPtr, ITreeNode> m_dynamicNodes = new Dictionary<IntPtr, ITreeNode>();
        List<IStaticTreeNode> m_allNodes = new List<IStaticTreeNode>();

        Dictionary<uint, ITreeNodeContext> m_contexts = new Dictionary<uint, ITreeNodeContext>();

        public STreeNodeMgr()
        {
            SharpHelper.RegisterSharpNodeCallback(OnNodeLoaded, OnNodeUpdate, OnContextInit, OnContextUpdate);
        }

        public void Clear()
        {
            m_allNodes.Clear();
            m_dynamicNodes.Clear();
            m_contexts.Clear();
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

        void OnContextInit(IntPtr pNode, int index, uint contextUID)
        {
            if (index < 0 || index >= m_allNodes.Count)
                return;
            var node = m_allNodes[index];
            if (node == null) return;
            IHasTreeNodeContext? hasTreeNodeContext = node as IHasTreeNodeContext;
            if (hasTreeNodeContext != null)
            {
                var context = hasTreeNodeContext.FetchContext();
                m_contexts.Add(contextUID, context);
                context.OnInit();
            }
        }

        NodeState OnContextUpdate(IntPtr pNode, IntPtr pAgent, int index, uint contextUID, NodeState lastState)
        {
            if (index < 0 || index >= m_allNodes.Count)
                return NodeState.NS_INVALID;
            var node = m_allNodes[index];
            if (node == null) return NodeState.NS_INVALID;
            if (m_contexts.TryGetValue(contextUID, out var context))
            {
                var res = context.OnNodeUpdate(pNode, pAgent, lastState);
                if (res != NodeState.NS_RUNNING && res != NodeState.NS_BREAK)
                {
                    IHasTreeNodeContext? hasTreeNodeContext = node as IHasTreeNodeContext;
                    if (hasTreeNodeContext != null)
                    {
                        hasTreeNodeContext.DestroyContext(context);
                    }
                    m_contexts.Remove(contextUID);
                }
                return res;
            }
            return NodeState.NS_INVALID;
        }
    }
}
