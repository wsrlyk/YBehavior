using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace YBehavior.Editor.Core.New
{
    /// <summary>
    /// A string describing an event
    /// </summary>
    public struct TransitionEvent : IEquatable<TransitionEvent>
    {
        private string m_Event;
        /// <summary>
        /// The event content
        /// </summary>
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
    /// <summary>
    /// A from and a to state make the key of mapping
    /// </summary>
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
    /// <summary>
    /// A TransitionEvent makes the value of mapping
    /// </summary>
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
    /// <summary>
    /// Types of transition
    /// </summary>
    public enum TransitionType
    {
        /// <summary>
        /// Normal transition
        /// </summary>
        Normal,
        /// <summary>
        /// Transition to the default state
        /// </summary>
        Default,
        /// <summary>
        /// Transition from the entry node
        /// </summary>
        Entry,
        /// <summary>
        /// Transition to the exit node
        /// </summary>
        Exit,
    }
    /// <summary>
    /// A transition between two exact states, containing multiple different TransitionEvents
    /// </summary>
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
        /// <summary>
        /// The from-state and to-state
        /// </summary>
        public TransitionMapKey Key { get; }
        /// <summary>
        /// Collection of events
        /// </summary>
        public ObservableCollection<TransitionMapValue> Value { get; } = new ObservableCollection<TransitionMapValue>();
        /// <summary>
        /// ViewModel
        /// </summary>
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
    /// <summary>
    /// Collection of transitions
    /// </summary>
    public class Transitions : System.Collections.IEnumerable
    {
        List<Transition> m_Trans = new List<Transition>();
        public System.Collections.IEnumerator GetEnumerator() { return m_Trans.GetEnumerator(); }
        /// <summary>
        /// Create a transition for a pair of states
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public Transition CreateEmpty(TransitionMapKey key)
        {
            var res = new Transition(key);
            m_Trans.Add(res);
            return res;
        }

        //public Transition Insert(TransitionMapKey key, TransitionMapValue value)
        //{
        //    var res = new Transition(key, value);
        //    m_Trans.Add(res);
        //    return res;
        //}

        /// <summary>
        /// Create a transition for a pair of states
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
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
        /// <summary>
        /// For Inserting from file
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="events"></param>
        /// <returns></returns>
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
        /// <summary>
        /// Remove a transition
        /// </summary>
        /// <param name="trans"></param>
        /// <returns></returns>
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
    /// <summary>
    /// ViewModel of transition
    /// </summary>
    public class TransitionRenderer : System.ComponentModel.INotifyPropertyChanged
    {
        /// <summary>
        /// Model
        /// </summary>
        public Transition Owner { get; set; }
        /// <summary>
        /// Collection of events
        /// </summary>
        public ObservableCollection<TransitionMapValue> Events
        {
            get { return Owner.Value; }
        }
        /// <summary>
        /// Name of the transition
        /// </summary>
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
