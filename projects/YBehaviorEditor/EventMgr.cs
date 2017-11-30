using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using YBehavior.Editor.Core;

namespace YBehavior.Editor
{
    public enum EventType
    {
        None,
        WorkBenchLoaded,
    }

    public class EventMgr
    {
        public static EventMgr Instance { get { return s_Instance; } }
        static EventMgr s_Instance = new EventMgr();

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

    public class WorkBenchLoadedArg:EventArg
    {
        public WorkBench Bench { get; set; }
        public override EventType Type => EventType.WorkBenchLoaded;
    }
}
