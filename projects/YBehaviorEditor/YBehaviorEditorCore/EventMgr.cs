using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using YBehavior.Editor.Core;

namespace YBehavior.Editor.Core
{
    public enum EventType
    {
        None,
        WorkBenchLoaded,
        NewNodeAdded,
        RemoveNode,
        NodeRemoved,
        NodesConnected,
        NodesDisconnected,
        SelectionChanged,
        SharedVariableChanged,
    }

    public class EventMgr : Singleton<EventMgr>
    {
        public delegate void EventHandler(EventArg arg);
        Dictionary<EventType, EventHandler> m_EventDic = new Dictionary<EventType, EventHandler>();

        public void Register(EventType type, EventHandler handler)
        {
            if (!m_EventDic.TryGetValue(type, out EventHandler exist))
            {
                exist = handler;
                m_EventDic.Add(type, exist);
            }
            else
            {
                exist += handler;
                m_EventDic[type] = exist;
            }
        }

        public void Send(EventArg arg)
        {
            if (m_EventDic.TryGetValue(arg.Type, out EventHandler exist))
            {
                exist(arg);
            }
        }
    }

    public class EventArg
    {

        public virtual EventType Type { get { return EventType.None; } }
    }

    public class WorkBenchLoadedArg : EventArg
    {
        public WorkBench Bench { get; set; }
        public override EventType Type => EventType.WorkBenchLoaded;
    }

    public class NewNodeAddedArg : EventArg
    {
        public Node Node { get; set; }
        public override EventType Type => EventType.NewNodeAdded;
    }

    /// <summary>
    /// Do the removing operation
    /// </summary>
    public class RemoveNodeArg : EventArg
    {
        public Node Node { get; set; }
        public override EventType Type => EventType.RemoveNode;
    }

    /// <summary>
    /// After the node is removed, notify others
    /// </summary>
    public class NodeRemovedArg : EventArg
    {
        public Node Node { get; set; }
        public override EventType Type => EventType.NodeRemoved;
    }

    public class NodesConnectedArg : EventArg
    {
        public ConnectionHolder Holder0 { get; set; }
        public ConnectionHolder Holder1 { get; set; }
        public override EventType Type => EventType.NodesConnected;
    }

    public class NodesDisconnectedArg : EventArg
    {
        public ConnectionHolder ChildHolder { get; set; }
        public override EventType Type => EventType.NodesDisconnected;
    }
    public class SelectionChangedArg : EventArg
    {
        public ISelectable Target { get; set; }
        public override EventType Type => EventType.SelectionChanged;
    }
    public class SharedVariableChangedArg : EventArg
    {
        public override EventType Type => EventType.SharedVariableChanged;
    }

}
