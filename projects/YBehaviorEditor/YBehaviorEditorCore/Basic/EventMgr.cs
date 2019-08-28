using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace YBehavior.Editor.Core.New
{
    public enum EventType
    {
        None,
        WorkBenchLoaded,
        WorkBenchSelected,
        SelectWorkBench,
        WorkBenchSaved,
        NewNodeAdded,
        NodeMoved,
        NodeDuplicated,
        SelectionChanged,
        SharedVariableChanged,
        NetworkConnectionChanged,
        DebugTargetChanged,
        TickResult,
        CommentCreated,
        ShowSystemTips,
        MakeCenter,
        PopMenu,
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
                exist -= handler;
                exist += handler;
                m_EventDic[type] = exist;
            }
        }

        public void Unregister(EventType type, EventHandler handler)
        {
            if (m_EventDic.TryGetValue(type, out EventHandler exist))
            {
                exist -= handler;
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

    public class WorkBenchSelectedArg : EventArg
    {
        public WorkBench Bench { get; set; }
        public override EventType Type => EventType.WorkBenchSelected;
    }

    public class SelectWorkBenchArg : EventArg
    {
        public WorkBench Bench { get; set; }
        public override EventType Type => EventType.SelectWorkBench;
    }

    public class WorkBenchSavedArg : EventArg
    {
        public WorkBench Bench { get; set; }
        public bool bCreate { get; set; }
        public override EventType Type => EventType.WorkBenchSaved;
    }

    public class NewNodeAddedArg : EventArg
    {
        public NodeBase Node { get; set; }
        public override EventType Type => EventType.NewNodeAdded;
    }

    /// <summary>
    /// After the node is moved, notify mainly the workbench to refresh the uid
    /// </summary>
    public class NodeMovedArg : EventArg
    {
        public NodeBase Node { get; set; }
        public override EventType Type => EventType.NodeMoved;
    }

    public class NodeDuplicatedArg : EventArg
    {
        public NodeBase Node { get; set; }
        public bool bIncludeChildren { get; set; } = false;
        public override EventType Type => EventType.NodeDuplicated;
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
    public class NetworkConnectionChangedArg : EventArg
    {
        public bool bConnected { get; set; }
        public override EventType Type => EventType.NetworkConnectionChanged;
    }
    public class DebugTargetChangedArg : EventArg
    {
        public override EventType Type => EventType.DebugTargetChanged;
    }
    public class TickResultArg : EventArg
    {
        //public bool bInstant { get; set; }
        public uint Token { get; set; }
        public override EventType Type => EventType.TickResult;
    }

    public class CommentCreatedArg : EventArg
    {
        public Comment Comment { get; set; }
        public override EventType Type => EventType.CommentCreated;
    }

    public class ShowSystemTipsArg : EventArg
    {
        public enum TipsType
        {
            TT_Error,
            TT_Success,
        }
        public string Content { get; set; }
        public TipsType TipType { get; set; } = TipsType.TT_Success;
        public override EventType Type => EventType.ShowSystemTips;
    }

    public class MakeCenterArg : EventArg
    {
        public override EventType Type => EventType.MakeCenter;
    }

    public class PopMenuArg : EventArg
    {
        public override EventType Type => EventType.PopMenu;

        public PopMenu Menu { get; set; }
        public System.Windows.Point Pos { get; set; }
    }
}
