using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace YBehavior.Editor.Core.New
{
    public struct TransitionEvent : IEquatable<TransitionEvent>
    {
        private string m_Event;
        public string Event
        {
            get { return m_Event; }
            set
            {
                if (value == null)
                    m_Event = string.Empty;
                else
                    m_Event = value;
            }
        }
        public TransitionEvent(string s)
        {
            m_Event = null;
            Event = s;
        }

        public bool Equals(TransitionEvent other)
        {
            return Event.Equals(other.Event);
        }
	};

    public struct TransitionMapKey : IEquatable<TransitionMapKey>
    {
        public FSMStateNode FromState { get; set; }
        public TransitionEvent Trans { get; set; }

        public TransitionMapKey(FSMStateNode from, string eventName)
        {
            FromState = from;
            Trans = new TransitionEvent(eventName);
        }

        public bool Equals(TransitionMapKey other)
        {
            return FromState == other.FromState && Trans.Equals(other.Trans);
        }

	};

    public struct TransitionMapValue
    {
        public FSMStateNode ToState { get; set; }
    };

    public class TransitionResult
    {
        public TransitionResult(TransitionMapKey key, TransitionMapValue value)
        {
            Key = key;
            Value = value;
        }
        public TransitionMapKey Key { get; set; }
        public TransitionMapValue Value { get; set; }
    }

    public class Transition : System.Collections.IEnumerable
    {
        Dictionary<TransitionMapKey, TransitionResult> m_Trans = new Dictionary<TransitionMapKey, TransitionResult>();
        public System.Collections.IEnumerator GetEnumerator() { return m_Trans.Values.GetEnumerator(); }

        public bool Insert(TransitionMapKey key, TransitionMapValue value)
        {
            try
            {
                m_Trans.Add(key, new TransitionResult(key, value));
            }
            catch (Exception e)
            {
                LogMgr.Instance.Error("Insert trans failed: " + e.ToString());
                return false;
            }
            return true;
        }

        public bool Insert(FSMStateNode from, string eventName, FSMStateNode to)
        {
            TransitionMapKey key = new TransitionMapKey(from, eventName);

            TransitionMapValue value = new TransitionMapValue
            {
                ToState = to
            };

            return Insert(key, value);
        }

        public void Remove(TransitionMapKey key)
        {
            m_Trans.Remove(key);
        }

        public TransitionResult GetTransition(TransitionMapKey key)
        {
            TransitionResult res;
            m_Trans.TryGetValue(key, out res);
            return res;
        }
    }
}
