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
        void OnInit();
        /// <summary>
        /// Called every tick
        /// </summary>
        /// <param name="pNode">Pointer to the node in cpp</param>
        /// <param name="pAgent">Pointer to the agent in cpp</param>
        /// <param name="lastState">The result of last node, usually used in branch node to get the result of child node</param>
        /// <returns></returns>
        ENodeState OnNodeUpdate(IntPtr pNode, IntPtr pAgent, ENodeState lastState);
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
        /// <returns></returns>
        ENodeState OnNodeUpdate(IntPtr pNode, IntPtr pAgent);
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

        Dictionary<IntPtr, IHasPin> m_dynamicNodes = new Dictionary<IntPtr, IHasPin>();
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

        bool OnNodeLoaded(IntPtr pNode, IntPtr pData, int index)
        {
            if (index < 0 || index >= m_allNodes.Count)
                return false;
            var node = m_allNodes[index];
            if (node == null) return false;
            if (!(node is IHasPin))
                return true;
            if (!m_dynamicNodes.TryGetValue(pNode, out var dynamicNode))
            {
                dynamicNode = Activator.CreateInstance(node.GetType()) as IHasPin;
                if (dynamicNode != null)
                    m_dynamicNodes.Add(pNode, dynamicNode);
                else
                    return false;
            }
            else
            {
                //multi load, should not run to here
            }
            return dynamicNode.OnNodeLoaded(pNode, pData);
        }

        ENodeState OnNodeUpdate(IntPtr pNode, IntPtr pAgent, int index)
        {
            if (TryGetNode(pNode, index, out var node))
            {
                return (node as INoTreeNodeContext).OnNodeUpdate(pNode, pAgent);
            }
            //not found, should not run to here
            return ENodeState.Invalid;
        }

        void OnContextInit(IntPtr pNode, int index, uint contextUID)
        {
            if (!TryGetNode(pNode, index, out var node))
                return;

            IHasTreeNodeContext? hasTreeNodeContext = node as IHasTreeNodeContext;
            if (hasTreeNodeContext != null)
            {
                var context = hasTreeNodeContext.CreateContext();
                m_contexts.Add(contextUID, context);
                context.OnInit();
            }
        }

        ENodeState OnContextUpdate(IntPtr pNode, IntPtr pAgent, int index, uint contextUID, ENodeState lastState)
        {
            if (!TryGetNode(pNode, index, out var node))
                return ENodeState.Invalid;

            if (m_contexts.TryGetValue(contextUID, out var context))
            {
                var res = context.OnNodeUpdate(pNode, pAgent, lastState);
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

        bool TryGetNode(IntPtr pNode, int index, out object? node)
        {
            node = null;
            if (index < 0 || index >= m_allNodes.Count)
                return false;
            node = m_allNodes[index];
            if (node == null) return false;
            if (!(node is IHasPin))
                return true;
            if (m_dynamicNodes.TryGetValue(pNode, out var dynamicNode))
            {
                node = dynamicNode;
                return true;
            }
            return false;
        }
    }
}
