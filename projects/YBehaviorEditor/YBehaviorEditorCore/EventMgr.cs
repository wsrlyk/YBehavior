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
        NodesConnected,
        NodesDisconnected,
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
}
