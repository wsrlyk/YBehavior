using YBehaviorSharp;

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace YBehaviorSharp
{
    /// <summary>
    /// To record running data for a treenode running for multiple frames
    /// </summary>
    public interface ITreeNodeContext
    {
        /// <summary>
        /// Called when created
        /// </summary>
        void OnInit();
        /// <summary>
        /// Called every tick
        /// </summary>
        /// <param name="pNode">Pointer to the node in cpp</param>
        /// <param name="pAgent">Pointer to the agent in cpp</param>
        /// <param name="agentIndex">Index of agent</param>
        /// <param name="lastState">The result of last node, usually used in branch node to get the result of child node</param>
        /// <returns></returns>
        ENodeState OnNodeUpdate(IntPtr pNode, IntPtr pAgent, int agentIndex, ENodeState lastState);
    }

    /// <summary>
    /// A tree node may keep running for multiple frames.
    /// A context is needed to record the states, etc.
    /// </summary>
    public interface IHasTreeNodeContext
    {
        /// <summary>
        /// Get the context object
        /// </summary>
        /// <returns></returns>
        ITreeNodeContext CreateContext();
        /// <summary>
        /// Recycle or destroy the context
        /// </summary>
        /// <param name="context"></param>
        void DestroyContext(ITreeNodeContext context);

    }
    /// <summary>
    /// No context, just use the shared tree node object
    /// </summary>
    public interface INoTreeNodeContext
    {
        /// <summary>
        /// Called every tick
        /// </summary>
        /// <param name="pNode">Pointer to the node in cpp</param>
        /// <param name="pAgent">Pointer to the agent in cpp</param>
        /// <param name="agentIndex">Index of agent</param>
        /// <returns></returns>
        ENodeState OnNodeUpdate(IntPtr pNode, IntPtr pAgent, int agentIndex);
    }

    /// <summary>
    /// Base interface of tree node. 
    /// </summary>
    public interface ITreeNode
    {
        /// <summary>
        /// Name of node
        /// </summary>
        string NodeName { get; }
    }

    /// <summary>
    /// Have a chance to load pins from config
    /// </summary>
    public interface IHasPin
    {
        /// <summary>
        /// Will be called only once when loaded
        /// </summary>
        /// <param name="pNode">Pointer to the node in cpp</param>
        /// <param name="pData">Pointer to the config in cpp</param>
        /// <returns></returns>
        bool OnNodeLoaded(IntPtr pNode, IntPtr pData);
    }
    /// <summary>
    /// Tree node that have pins and context
    /// </summary>
    public interface ITreeNodeWithPinContext : ITreeNode, IHasPin, IHasTreeNodeContext { }
    /// <summary>
    /// Tree node that have pins but no context
    /// </summary>
    public interface ITreeNodeWithPin : ITreeNode, IHasPin, INoTreeNodeContext { }
    /// <summary>
    /// Tree node that have context but no pins
    /// </summary>
    public interface ITreeNodeWithContext : ITreeNode, IHasTreeNodeContext { }
    /// <summary>
    /// Tree node that have no context or pins
    /// </summary>
    public interface ITreeNodeWithNothing : ITreeNode, INoTreeNodeContext { }

    internal partial class SUtility
    {
        [DllImport(VERSION.dll)]
        public static extern void RegisterSharpNode(string name, int index, bool hasContext);

        [DllImport(VERSION.dll)]
        public static extern void RegisterSharpNodeCallback(
            OnNodeLoaded onNodeLoaded,
            OnNodeUpdate onNodeUpdate,
            OnNodeContextInit onContextInit,
            OnNodeContextUpdate onContextUpdate);
    }
    public partial class SharpHelper
    { 
        /// <summary>
        /// Every treenode in C# should be registered by this function at the start of the game
        /// </summary>
        /// <param name="node"></param>
        public static bool RegisterTreeNode(ITreeNode node)
        {
            int index = STreeNodeMgr.Instance.Register(node);
            if (index < 0)
                return false;
            SUtility.RegisterSharpNode(node.NodeName, index, node is IHasTreeNodeContext);
            return true;
        }
    }

    internal class STreeNodeMgr
    {
        public static STreeNodeMgr Instance { get; private set; } = new STreeNodeMgr();

        List<IHasPin> m_dynamicNodes = new List<IHasPin>();
        List<ITreeNode> m_allNodes = new List<ITreeNode>();

        Dictionary<uint, ITreeNodeContext> m_contexts = new Dictionary<uint, ITreeNodeContext>();

        OnNodeLoaded m_onNodeLoaded;
        OnNodeUpdate m_onNodeUpdate;
        OnNodeContextInit m_onContextInit;
        OnNodeContextUpdate m_onContextUpdate;
        public STreeNodeMgr()
        {
            m_onNodeLoaded = OnNodeLoaded;
            m_onNodeUpdate = OnNodeUpdate;
            m_onContextInit = OnContextInit;
            m_onContextUpdate = OnContextUpdate;
            SUtility.RegisterSharpNodeCallback(m_onNodeLoaded, m_onNodeUpdate, m_onContextInit, m_onContextUpdate);
        }

        public void Clear()
        {
            m_allNodes.Clear();
            m_dynamicNodes.Clear();
            m_contexts.Clear();
        }
        public int Register(ITreeNode node)
        {
            if (node == null)
                return -1;

            if ((node is IHasTreeNodeContext) ^ (node is INoTreeNodeContext))
            {
                m_allNodes.Add(node);
                return m_allNodes.Count - 1;
            }
            else
            {
                return -1;
            }
        }

        int OnNodeLoaded(IntPtr pNode, IntPtr pData, int index)
        {
            if (index < 0 || index >= m_allNodes.Count)
                return -2;
            var node = m_allNodes[index];
            if (node == null) return -2;
            if (!(node is IHasPin))
                return -1;
            int dynamicIndex = m_dynamicNodes.Count;
            var dynamicNode = Activator.CreateInstance(node.GetType()) as IHasPin;
            if (dynamicNode != null)
                m_dynamicNodes.Add(dynamicNode);
            else
                return -2;

            if (!dynamicNode.OnNodeLoaded(pNode, pData))
                return -2;
            return dynamicIndex;
        }

        ENodeState OnNodeUpdate(IntPtr pNode, IntPtr pAgent, int agentIndex, int staticIndex, int dynamicIndex)
        {
            if (TryGetNode(staticIndex, dynamicIndex, out var node))
            {
                return (node as INoTreeNodeContext).OnNodeUpdate(pNode, pAgent, agentIndex);
            }
            //not found, should not run to here
            return ENodeState.Invalid;
        }

        void OnContextInit(IntPtr pNode, int staticIndex, int dynamicIndex, uint contextUID)
        {
            if (!TryGetNode(staticIndex, dynamicIndex, out var node))
                return;

            IHasTreeNodeContext? hasTreeNodeContext = node as IHasTreeNodeContext;
            if (hasTreeNodeContext != null)
            {
                var context = hasTreeNodeContext.CreateContext();
                m_contexts.Add(contextUID, context);
                context.OnInit();
            }
        }

        ENodeState OnContextUpdate(IntPtr pNode, IntPtr pAgent, int agentIndex, int staticIndex, int dynamicIndex, uint contextUID, ENodeState lastState)
        {
            if (!TryGetNode(staticIndex, dynamicIndex, out var node))
                return ENodeState.Invalid;

            if (m_contexts.TryGetValue(contextUID, out var context))
            {
                var res = context.OnNodeUpdate(pNode, pAgent, agentIndex, lastState);
                if (res != ENodeState.Running && res != ENodeState.Break)
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
            return ENodeState.Invalid;
        }

        bool TryGetNode(int staticIndex, int dynamicIndex, out object? node)
        {
            node = null;
            if (dynamicIndex >= 0 && dynamicIndex < m_dynamicNodes.Count)
                node = m_dynamicNodes[dynamicIndex];
            else if (staticIndex >= 0 && staticIndex < m_allNodes.Count)
                node = m_allNodes[staticIndex];
            return node != null;
        }
    }
}
