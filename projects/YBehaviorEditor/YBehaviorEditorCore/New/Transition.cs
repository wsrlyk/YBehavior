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

            ///> AnyState must convert to NULL
            if (from is FSMAnyStateNode)
            {
                //FromState = to.OwnerMachine.AnyState;
                FromState = null;
            }
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

    public class TransitionMapValue
    {
        public TransitionMapValue(string e)
        {
            Event = new TransitionEvent(e);
        }
        public TransitionEvent Event { get; set; }

        public string Value
        {
            get { return Event.Event; }
            set
            {
                Event = new TransitionEvent(value);
            }
        }
    };

    public enum TransitionType
    {
        Normal,
        Default,
        Entry,
        Exit,
    }

    public class Transition
    {
        public Transition(TransitionMapKey key, TransitionMapValue value)
        {
            Key = key;
            Value.Add(value);
            Renderer.Owner = this;

            _Init();
        }
        public Transition(TransitionMapKey key)
        {
            Key = key;
            Renderer.Owner = this;

            _Init();
        }

        public TransitionMapKey Key { get; }
        public ObservableCollection<TransitionMapValue> Value { get; } = new ObservableCollection<TransitionMapValue>();

        public TransitionRenderer Renderer { get; set; } = new TransitionRenderer();
        public TransitionType Type = TransitionType.Normal;

        void _Init()
        {
            if (Key.FromState != null && Key.FromState is FSMEntryStateNode)
                Type = TransitionType.Entry;
            else if (Key.ToState != null && Key.ToState is FSMExitStateNode)
                Type = TransitionType.Exit;
        }
    }

    public class Transitions : System.Collections.IEnumerable
    {
        List<Transition> m_Trans = new List<Transition>();
        public System.Collections.IEnumerator GetEnumerator() { return m_Trans.GetEnumerator(); }

        public Transition CreateEmpty(TransitionMapKey key)
        {
            var res = new Transition(key);
            m_Trans.Add(res);
            return res;
        }

        public Transition Insert(TransitionMapKey key, TransitionMapValue value)
        {
            var res = new Transition(key, value);
            m_Trans.Add(res);
            return res;
        }

        public Transition Insert(FSMStateNode from, FSMStateNode to)
        {
            TransitionMapKey key = new TransitionMapKey(from, to);

            return CreateEmpty(key);
        }

        /// <summary>
        /// Insert exist trans, mainly for Redo/Undo
        /// </summary>
        /// <param name="trans"></param>
        /// <returns></returns>
        public Transition Insert(Transition trans)
        {
            m_Trans.Add(trans);
            return trans;
        }

        public Transition Insert(FSMStateNode from, FSMStateNode to, List<string> events)
        {
            TransitionMapKey key = new TransitionMapKey(from, to);

            if (events.Count == 0)
                return CreateEmpty(key);

            Transition res = CreateEmpty(key);
            foreach (string s in events)
            {
                TransitionMapValue value = new TransitionMapValue(s);
                res.Value.Add(value);
            }
            return res;
        }

        public bool Remove(Transition trans)
        {
            return m_Trans.Remove(trans);
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
        public Transition Owner { get; set; }

        public ObservableCollection<TransitionMapValue> Events
        {
            get { return Owner.Value; }
        }

        public string Name
        {
            get
            {
                return string.Format("{0} => {1} {2}"
                    , Owner.Key.FromState == null ? "Any" : Owner.Key.FromState.ForceGetRenderer.FullName
                    , Owner.Key.ToState == null ? "Any" : Owner.Key.ToState.ForceGetRenderer.FullName
                    , Owner.Type == TransitionType.Default ? "(Default)" : string.Empty);
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
