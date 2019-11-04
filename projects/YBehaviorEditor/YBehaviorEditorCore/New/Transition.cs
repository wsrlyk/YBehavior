using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

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
        public FSMStateNode ToState { get; set; }

        public TransitionMapKey(FSMStateNode from, FSMStateNode to)
        {
            FromState = from;
            ToState = to;
        }

        public bool Equals(TransitionMapKey other)
        {
            return EqualState(FromState, other.FromState)
                && EqualState(ToState, other.ToState);
        }

        private bool EqualState(FSMStateNode s0, FSMStateNode s1)
        {
            return
                s0 == s1
                || ((s0 == null || s0 is FSMAnyStateNode)
                && (s1 == null || s1 is FSMAnyStateNode));
        }
	};

    public struct TransitionMapValue
    {
        public TransitionMapValue(string e)
        {
            Event = new TransitionEvent(e);
        }
        public TransitionEvent Event { get; set; }
    };

    public class TransitionResult
    {
        public TransitionResult(TransitionMapKey key, TransitionMapValue value)
        {
            Key = key;
            Value.Add(value);

            Renderer.Owner = this;
        }
        public TransitionResult(TransitionMapKey key)
        {
            Key = key;

            Renderer.Owner = this;
        }
        public TransitionMapKey Key { get; set; }
        public ObservableCollection<TransitionMapValue> Value { get; set; } = new ObservableCollection<TransitionMapValue>();

        public TransitionRenderer Renderer { get; set; } = new TransitionRenderer();
    }

    public class Transitions : System.Collections.IEnumerable
    {
        Dictionary<TransitionMapKey, TransitionResult> m_Trans = new Dictionary<TransitionMapKey, TransitionResult>();
        public System.Collections.IEnumerator GetEnumerator() { return m_Trans.Values.GetEnumerator(); }

        public TransitionResult CreateEmpty(TransitionMapKey key)
        {
            if (!m_Trans.TryGetValue(key, out var res))
            {
                res = new TransitionResult(key);
                m_Trans.Add(key, res);
                return res;
            }
            return null;
        }

        public TransitionResult Insert(TransitionMapKey key, TransitionMapValue value)
        {
            if (!m_Trans.TryGetValue(key, out var res))
            {
                res = new TransitionResult(key, value);
                m_Trans.Add(key, res);
            }
            else
            {
                if(res.Value.Contains(value))
                {
                    LogMgr.Instance.Error("Insert trans failed: " + value.Event);
                    return null;
                }
                else
                {
                    res.Value.Add(value);
                }
            }
            return res;
        }

        public TransitionResult Insert(FSMStateNode from, string eventName, FSMStateNode to)
        {
            TransitionMapKey key = new TransitionMapKey(from, to);

            TransitionMapValue value = new TransitionMapValue(eventName);

            return Insert(key, value);
        }

        public TransitionResult Insert(FSMStateNode from, FSMStateNode to, List<string> events)
        {
            TransitionMapKey key = new TransitionMapKey(from, to);

            if (events.Count == 0)
                return CreateEmpty(key);

            TransitionResult res = null;
            foreach (string s in events)
            {
                TransitionMapValue value = new TransitionMapValue(s);
                res = Insert(key, value);
            }
            return res;
        }

        public void Remove(TransitionResult trans)
        {
            m_Trans.Remove(trans.Key);
        }

        //public TransitionResult GetTransition(TransitionMapKey key)
        //{
        //    TransitionResult res;
        //    m_Trans.TryGetValue(key, out res);
        //    return res;
        //}
    }

    public class TransitionRenderer : System.ComponentModel.INotifyPropertyChanged
    {
        public TransitionResult Owner { get; set; }

        public ObservableCollection<TransitionMapValue> Events
        {
            get { return Owner.Value; }
        }

        public string Name
        {
            get
            {
                return string.Format("{0} => {1}"
                    , Owner.Key.FromState == null ? "Any" : Owner.Key.FromState.ForceGetRenderer.FullName
                    , Owner.Key.ToState == null ? "Any" : Owner.Key.ToState.ForceGetRenderer.FullName);
            }
        }

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        internal protected void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
