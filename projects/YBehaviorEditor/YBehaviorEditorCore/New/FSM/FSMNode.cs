using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Xml;

namespace YBehavior.Editor.Core.New
{
    /// <summary>
    /// FSM node management
    /// </summary>
    public class FSMNodeMgr : Singleton<FSMNodeMgr>
    {
        List<FSMStateNode> m_StateList = new List<FSMStateNode>();
        /// <summary>
        /// All kinds of states
        /// </summary>
        public List<FSMStateNode> NodeList { get { return m_StateList; } }
        private Dictionary<string, Type> m_TypeDic = new Dictionary<string, Type>();

        public FSMNodeMgr()
        {
            var subTypeQuery = from t in System.Reflection.Assembly.GetExecutingAssembly().GetTypes()
                               where Utility.IsSubClassOf(t, typeof(FSMStateNode))
                               select t;

            foreach (var type in subTypeQuery)
            {
                FSMStateNode node = Activator.CreateInstance(type) as FSMStateNode;
                if (node.Type == FSMStateType.Invalid)
                    continue;
                //node.LoadDescription();
                m_StateList.Add(node);
                m_TypeDic.Add(node.Name, type);
                //LogMgr.Instance.Log(type.ToString());
            }
        }
        /// <summary>
        /// Create state by name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public FSMStateNode CreateStateByName(string name)
        {
            FSMStateNode node = null;
            if (m_TypeDic.TryGetValue(name, out Type type))
            {
                node = Activator.CreateInstance(type) as FSMStateNode;
                node.CreateBase();
                //node.LoadDescription();
                return node;
            }

            return null;
        }
        /// <summary>
        /// Create state by type
        /// </summary>
        /// <typeparam name="T">FSMNode type</typeparam>
        /// <returns></returns>
        public T CreateNode<T>() where T: FSMNode, new()
        {
            T t = new T();
            t.CreateBase();
            return t;
        }
    }
    /// <summary>
    /// A wrapper to the fsm node
    /// </summary>
    public class FSMNodeWrapper : NodeWrapper
    {
        public FSM FSM { get { return Graph as FSM; } }
        FSMRootMachineNode m_RootMachine;
        /// <summary>
        /// The root of fsm
        /// </summary>
        public FSMRootMachineNode RootMachine { get { return m_RootMachine; } }

        FSMMachineNode m_OwnerMachine;
        /// <summary>
        /// The machine this node belongs to
        /// </summary>
        public FSMMachineNode OwnerMachine { get { return m_OwnerMachine; } }
        /// <summary>
        /// Change the owner
        /// </summary>
        /// <param name="machine"></param>
        public void SetOwner(FSMMachineNode machine)
        {
            m_OwnerMachine = machine;
            if (machine == null)
            {
                if (Node is FSMRootMachineNode)
                    m_RootMachine = Node as FSMRootMachineNode;
                else
                {
                    LogMgr.Instance.Error("A SubMachine/State belongs to none of the root machines: " + Node.ToString());
                }
            }
            else
            {
                m_RootMachine = machine.RootMachine;
            }
        }
    }
    /// <summary>
    /// FSM node class
    /// </summary>
    public class FSMNode : NodeBase
    {
        protected override void _CreateWrapper()
        {
            m_Wrapper = new FSMNodeWrapper();
        }
        /// <summary>
        /// Get the root machine
        /// </summary>
        public FSMRootMachineNode RootMachine
        {
            get { return (m_Wrapper as FSMNodeWrapper).RootMachine; }
        }
        /// <summary>
        /// The machine this node belongs to
        /// </summary>
        public FSMMachineNode OwnerMachine
        {
            get { return (m_Wrapper as FSMNodeWrapper).OwnerMachine; }
            set { (m_Wrapper as FSMNodeWrapper).SetOwner(value); }
        }
    }
    /// <summary>
    /// The machine node, containing multiple states
    /// </summary>
    public class FSMMachineNode : FSMNode
    {
        /// <summary>
        /// State pointing to the sub machine
        /// </summary>
        public FSMMetaStateNode MetaState { get; set; }

        List<FSMStateNode> m_States = new List<FSMStateNode>();
        FSMStateNode m_Entry;
        FSMStateNode m_Exit;
        FSMStateNode m_Default;
        FSMStateNode m_Any;
        FSMStateNode m_Upper;
        /// <summary>
        /// Collection of states
        /// </summary>
        public List<FSMStateNode> States { get { return m_States; } }
        /// <summary>
        /// Entry to the machine
        /// </summary>
        public FSMStateNode EntryState { get { return m_Entry; } }
        /// <summary>
        /// Exit to the machine
        /// </summary>
        public FSMStateNode ExitState { get { return m_Exit; } }
        /// <summary>
        /// After entry, the default state the machine will go to
        /// </summary>
        public FSMStateNode DefaultState { get { return m_Default; } }
        /// <summary>
        /// State meaning any other states.
        /// Used for creating connections from any states
        /// </summary>
        public FSMStateNode AnyState { get { return m_Any; } }
        /// <summary>
        /// State meaning the parent machine.
        /// Used for creating connections with parents
        /// </summary>
        public FSMStateNode UpperState { get { return m_Upper; } }

        Transitions m_LocalTransition = new Transitions();
        /// <summary>
        /// Collections of transitions
        /// </summary>
        public Transitions LocalTransition { get { return m_LocalTransition; } }
        /// <summary>
        /// Level of machine, start from 0
        /// </summary>
        public uint Level { get; set; } = 0;

        public FSMMachineNode()
        {
        }

        public override void CreateBase()
        {
            base.CreateBase();
            _CreateEmpty();
        }

        void _CreateEmpty()
        {
            _ForceAddSpecialState(ref m_Entry, FSMStateNode.TypeEntry);
            _ForceAddSpecialState(ref m_Exit, FSMStateNode.TypeExit);
            _ForceAddSpecialState(ref m_Any, FSMStateNode.TypeAny);
        }
        /// <summary>
        /// Create upper state for non-root machine
        /// </summary>
        public void CreateUpper()
        {
            if (OwnerMachine != null)
                _ForceAddSpecialState(ref m_Upper, FSMStateNode.TypeUpper);
        }

        protected override void _CreateRenderer()
        {
            m_Renderer = new FSMMachineRenderer(this);
        }

        //public bool PreLoad()
        //{
        //    return true;
        //}

        /// <summary>
        /// Set default, and build connections
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public bool PostLoad(XmlNode data)
        {
            var attr = data.Attributes["Default"];
            if (attr != null)
            {
                FSMStateNode state = FindState(attr.Value);
                if (state!= null)
                {
                    FSMConnection oldConn = null;
                    FSMConnection newConn = null;
                    SetDefault(state, ref oldConn, ref newConn);
                }
                else
                {
                    LogMgr.Instance.Error("Cant find state as default: " + attr.Value);
                    return false;
                }
            }

            _BuildConnections(m_LocalTransition);

            return true;
        }
        /// <summary>
        /// Find a state by name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public FSMStateNode FindState(string name)
        {
            foreach (var state in m_States)
            {
                if ((state.Type == FSMStateType.User && state.NickName == name)
                    || (state.Type == FSMStateType.Special && state.Name == name))
                    return state;
            }
            return null;
        }

        static int s_StateSortIndex = 0;
        /// <summary>
        /// Remove a state
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public bool RemoveState(FSMStateNode state)
        {
            if (state.Type == FSMStateType.Special)
                return false;

            m_States.Remove(state);
            if (state.IsUserState)
            {
                if (m_Default == state)
                    m_Default = null;
                return RootMachine.RemoveGlobalState(state);
            }
            return true;
        }
        /// <summary>
        /// Add a state
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public bool AddState(FSMStateNode state)
        {
            state.OwnerMachine = this;
            state.SortIndex = ++s_StateSortIndex;

            m_States.Add(state);
            state.OnAddToMachine();

            if (state.Type == FSMStateType.Special)
            {
                if (state.Name == FSMStateNode.TypeEntry)
                    _ProcessSpecialState(ref m_Entry, state);
                else if (state.Name == FSMStateNode.TypeExit)
                    _ProcessSpecialState(ref m_Exit, state);
                else if (state.Name == FSMStateNode.TypeAny)
                    _ProcessSpecialState(ref m_Any, state);
                else if (state.Name == FSMStateNode.TypeUpper)
                    _ProcessSpecialState(ref m_Upper, state);
            }
            if (state.IsUserState)
                return RootMachine.AddGlobalState(state);

            return true;
        }
        /// <summary>
        /// Set default
        /// </summary>
        /// <param name="state"></param>
        /// <param name="oldConn">The connection between entry to old default state</param>
        /// <param name="newConn">The connection between entry and this state</param>
        /// <returns></returns>
        public bool SetDefault(FSMStateNode state, ref FSMConnection oldConn, ref FSMConnection newConn)
        {
            if (state != null)
            {
                if (state.OwnerMachine != this)
                    return false;
                if (state.Type == FSMStateType.Special)
                    return false;
            }

            if (m_Default != null)
            {
                Connection.FromTo fromto = new Connection.FromTo
                {
                    From = EntryState.Conns.GetConnector(Connector.IdentifierChildren, Connector.PosType.CHILDREN),
                    To = m_Default.Conns.ParentConnector
                };

                FSMConnection conn = fromto.From.FindConnection(fromto) as FSMConnection;
                if (conn == null)
                {
                    LogMgr.Instance.Error("There's a default state but cant find the connection");
                    return false;
                }
                Transition defaultTrans = null;
                foreach (var trans in conn.Trans)
                {
                    if (trans.Type == TransitionType.Default)
                    {
                        defaultTrans = trans;
                        break;
                    }
                }
                if (defaultTrans == null)
                {
                    LogMgr.Instance.Error("There's a default state but cant find the transition");
                    return false;
                }
                conn.Trans.Remove(defaultTrans);
                if (conn.Trans.Count == 0)
                {
                    Connector.TryDisconnect(fromto);
                }

                oldConn = conn;
                var oldState = m_Default;
                m_Default = null;
                oldState.PropertyChange(RenderProperty.DefaultState);
            }
            /// new default
            if (state != null)
            {
                Connector parent;
                Connector child;

                FSMConnection conn = Connector.TryConnect(EntryState.Conns.GetConnector(Connector.IdentifierChildren, Connector.PosType.CHILDREN), state.Conns.ParentConnector, out parent, out child) as FSMConnection;
                if (conn != null)
                {
                    Transition res = new Transition(new TransitionMapKey(EntryState, state));
                    res.Type = TransitionType.Default;
                    conn.Trans.Add(res);

                }
                else
                {
                    return false;
                }
                newConn = conn;
            }

            m_Default = state;
            if (m_Default != null)
                m_Default.PropertyChange(RenderProperty.DefaultState);
            return true;
        }

        void _ProcessSpecialState(ref FSMStateNode target, FSMStateNode state)
        {
            if (target != null)
            {
                LogMgr.Instance.Error("Duplicated special node " + target.ToString());
            }
            target = state;
        }

        void _ForceAddSpecialState(ref FSMStateNode target, string name)
        {
            if (target != null)
                return;
            FSMStateNode state = FSMNodeMgr.Instance.CreateStateByName(name);
            NodeBase.OnAddToGraph(state, Graph);
            //Utility.OperateNode(state, Graph, false, NodeBase.OnAddToGraph);

            AddState(state);
        }
        /// <summary>
        /// Create transitions from file
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="events"></param>
        /// <returns></returns>
        public bool TryAddTrans(string from, string to, List<string> events)
        {
            FSMStateNode fromState = null;
            if (!string.IsNullOrEmpty(from))
            {
                fromState = RootMachine.FindGloablState(from);
                if (fromState == null)
                {
                    LogMgr.Instance.Error("Cant find state: " + from);
                    return false;
                }
            }

            FSMStateNode toState = null;
            if (!string.IsNullOrEmpty(to))
            {
                toState = RootMachine.FindGloablState(to);
                if (toState == null)
                {
                    LogMgr.Instance.Error("Cant find state: " + to);
                    return false;
                }
            }
            else return false;

            if (RootMachine.Transition.Insert(fromState, toState, events) == null)
                return false;

            ///> TODO: build connection
            
            return true;
        }
        /// <summary>
        /// Create transitions from file
        /// </summary>
        /// <param name="to"></param>
        /// <param name="events"></param>
        /// <returns></returns>
        public bool TryAddEntryTrans(string to, List<string> events)
        {
            FSMStateNode fromState = this.EntryState;
            if (fromState == null)
                return false;

            FSMStateNode toState = null;
            if (!string.IsNullOrEmpty(to))
            {
                toState = RootMachine.FindGloablState(to);
                if (toState == null)
                {
                    LogMgr.Instance.Error("Cant find state: " + to);
                    return false;
                }
            }
            else return false;
            if (LocalTransition.Insert(fromState, toState, events) == null)
                return false;

            return true;
        }
        /// <summary>
        /// Create transitions from file
        /// </summary>
        /// <param name="to"></param>
        /// <param name="events"></param>
        /// <returns></returns>
        public bool TryAddExitTrans(string from, List<string> events)
        {
            FSMStateNode toState = this.ExitState;
            if (toState == null)
                return false;

            FSMStateNode fromState = null;
            if (!string.IsNullOrEmpty(from))
            {
                fromState = RootMachine.FindGloablState(from);
                if (fromState == null)
                {
                    LogMgr.Instance.Error("Cant find state: " + from);
                    return false;
                }
            }
            else return false;
            if (LocalTransition.Insert(fromState, toState, events) == null)
                return false;

            return true;
        }

        protected void _BuildConnections(Transitions transitions)
        {
            foreach (Transition t in transitions)
            {
                FSMStateNode fromState = t.Key.FromState;
                FSMStateNode toState = t.Key.ToState;

                var res = Utility.FindTransRoute(fromState, toState);
                foreach (var p in res.Route)
                {
                    _BuildConnection(p.Key, p.Value, t);
                }
            }
        }

        void _BuildConnection(FSMStateNode fromState, FSMStateNode toState, Transition trans)
        {
            if (fromState == null || toState == null || fromState.OwnerMachine != toState.OwnerMachine)
                return;

            FSMConnection conn = fromState.Conns.Connect(toState, Connector.IdentifierChildren) as FSMConnection;
            conn.Trans.Add(trans);
        }

        public bool RemoveLocalTrans(Transition t)
        {
            return _RemoveTrans(t, m_LocalTransition);
        }

        protected bool _RemoveTrans(Transition t, Transitions transitions)
        {
            if (!transitions.Remove(t))
                return false;
            FSMStateNode fromState = t.Key.FromState;
            FSMStateNode toState = t.Key.ToState;

            var res = Utility.FindTransRoute(fromState, toState);
            foreach (var p in res.Route)
            {
                _RemoveConnection(p.Key, p.Value, t);
            }

            return true;
        }

        void _RemoveConnection(FSMStateNode fromState, FSMStateNode toState, Transition trans)
        {
            if (fromState == null || toState == null || fromState.OwnerMachine != toState.OwnerMachine)
                return;
            Connection.FromTo fromto = new Connection.FromTo
            {
                From = fromState.Conns.GetConnector(Connector.IdentifierChildren, Connector.PosType.CHILDREN),
                To = toState.Conns.ParentConnector
            };

            FSMConnection conn = fromto.From.FindConnection(fromto) as FSMConnection;
            if (conn == null)
                return;

            conn.Trans.Remove(trans);
            if (conn.Trans.Count == 0)
                Connector.TryDisconnect(fromto);
        }

        TransitionResult _MakeLocalTrans(FSMStateNode fromState, FSMStateNode toState)
        {
            return _MakeTrans(fromState, toState, m_LocalTransition);
        }

        TransitionResult _MakeLocalTrans(Transition existTrans)
        {
            return _MakeTrans(existTrans, m_LocalTransition);
        }

        protected TransitionResult _MakeTrans(FSMStateNode fromState, FSMStateNode toState, Transitions transitions)
        {
            Transition trans = transitions.Insert(fromState, toState);

            return _OnTransMade(trans);
        }

        protected TransitionResult _MakeTrans(Transition existTrans, Transitions transitions)
        {
            Transition trans = transitions.Insert(existTrans);

            return _OnTransMade(trans);
        }

        protected TransitionResult _OnTransMade(Transition trans)
        {
            if (trans == null)
                return new TransitionResult();

            ///> FromState may change if it's an AnyState
            TransitionResult res;
            var route = Utility.FindTransRoute(trans.Key.FromState, trans.Key.ToState);
            foreach (var p in route.Route)
            {
                _MakeTransInRoute(p.Key, p.Value, trans);
            }
            res.Trans = trans;
            res.Route = route;
            return res;
        }

        void _MakeTransInRoute(FSMStateNode fromState, FSMStateNode toState, Transition t)
        {
            Connector parent;
            Connector child;
            FSMConnection conn = Connector.TryConnect(fromState.Conns.GetConnector(Connector.IdentifierChildren, Connector.PosType.CHILDREN), toState.Conns.ParentConnector, out parent, out child) as FSMConnection;
            if (conn != null)
                conn.Trans.Add(t);
        }
        /// <summary>
        /// Remove a transition
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public bool RemoveTrans(Transition t)
        {
            if ((t.Key.FromState != null && t.Key.FromState.Type == FSMStateType.Special && !(t.Key.FromState is FSMAnyStateNode))
                || (t.Key.ToState != null && t.Key.ToState.Type == FSMStateType.Special))
                return RemoveLocalTrans(t);
            return RootMachine.RemoveGlobalTrans(t);
        }
        /// <summary>
        /// Make a transition between two states
        /// </summary>
        /// <param name="fromState"></param>
        /// <param name="toState"></param>
        /// <returns></returns>
        public TransitionResult MakeTrans(FSMStateNode fromState, FSMStateNode toState)
        {
            if ((fromState != null && fromState.Type == FSMStateType.Special && !(fromState is FSMAnyStateNode))
                || (toState != null && toState.Type == FSMStateType.Special))
                return _MakeLocalTrans(fromState, toState);
            return RootMachine.MakeGlobalTrans(fromState, toState);
        }
        /// <summary>
        /// Add or create a new transition
        /// </summary>
        /// <param name="existTrans">if null, a new transition will be created. Or a transition will be added to it</param>
        /// <returns></returns>
        public TransitionResult MakeTrans(Transition existTrans)
        {
            if (existTrans == null)
                return new TransitionResult();
            if ((existTrans.Key.FromState != null && existTrans.Key.FromState.Type == FSMStateType.Special && !(existTrans.Key.FromState is FSMAnyStateNode))
                || (existTrans.Key.ToState != null && existTrans.Key.ToState.Type == FSMStateType.Special))
                return _MakeLocalTrans(existTrans);
            return RootMachine.MakeGlobalTrans(existTrans);
        }

        public override bool CheckValid()
        {
            bool res = true;
            bool hasUserState = false;
            foreach (FSMStateNode state in m_States)
            {
                hasUserState |= state.IsUserState;
                res &= state.CheckValid();
            }
            if (m_Default == null && hasUserState)
            {
                LogMgr.Instance.Error(string.Format("{0} must have a DefaultState", ForceGetRenderer.UITitle));
                res = false;
            }
            return res;
        }
    }
    /// <summary>
    /// Describe the result of making a transition
    /// </summary>
    public struct TransitionResult
    {
        /// <summary>
        /// The newly created or updated transition
        /// </summary>
        public Transition Trans;
        /// <summary>
        /// The route of the new transition
        /// </summary>
        public TransRoute Route;
    }
    /// <summary>
    /// The root machine node.
    /// It will have all states to make global transition.
    /// </summary>
    public class FSMRootMachineNode : FSMMachineNode
    {
        /// <summary>
        /// All states
        /// </summary>
        List<FSMStateNode> m_AllStates = new List<FSMStateNode>();
        Transitions m_Transition = new Transitions();
        /// <summary>
        /// All transitions
        /// </summary>
        public Transitions Transition { get { return m_Transition; } }

        public override void CreateBase()
        {
            base.CreateBase();
            OwnerMachine = null;
        }
        /// <summary>
        /// Remove a state from global list
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public bool RemoveGlobalState(FSMStateNode state)
        {
            if (state == null/* || string.IsNullOrEmpty(state.NickName)*/)
                return false;
            try
            {
                ///> TODO: check duplicate
                m_AllStates.Remove(state);
            }
            catch (Exception e)
            {
                LogMgr.Instance.Error("Remove global state failed: " + e.ToString());
                return false;
            }

            return true;
        }
        /// <summary>
        /// Add a state to global list
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public bool AddGlobalState(FSMStateNode state)
        {
            if (state == null/* || string.IsNullOrEmpty(state.NickName)*/)
                return false;
            try
            {
                ///> TODO: check duplicate
                m_AllStates.Add(state);
            }
            catch(Exception e)
            {
                LogMgr.Instance.Error("Add global state failed: " + e.ToString());
                return false;
            }

            return true;
        }
        /// <summary>
        /// Find a state by name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public FSMStateNode FindGloablState(string name)
        {
            //FSMStateNode state;
            foreach (var node in m_AllStates)
            {
                if (node.NickName == name)
                    return node;
            }
            LogMgr.Instance.Error("Cant find state: " + name);
            //m_AllStates.TryGetValue(name, out state);
            return null;
            //return state;
        }
        /// <summary>
        /// Build all local connections based on global connections after loading file
        /// </summary>
        public void BuildConnections()
        {
            _BuildConnections(m_Transition);
        }
        /// <summary>
        /// Remove a global transition
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public bool RemoveGlobalTrans(Transition t)
        {
            return _RemoveTrans(t, m_Transition);
        }
        /// <summary>
        /// Make a global transition
        /// </summary>
        /// <param name="fromState"></param>
        /// <param name="toState"></param>
        /// <returns></returns>
        public TransitionResult MakeGlobalTrans(FSMStateNode fromState, FSMStateNode toState)
        {
            return _MakeTrans(fromState, toState, m_Transition);
        }
        /// <summary>
        /// Create a new or add to an exist global transition
        /// </summary>
        /// <param name="existTrans"></param>
        /// <returns></returns>
        public TransitionResult MakeGlobalTrans(Transition existTrans)
        {
            return _MakeTrans(existTrans, m_Transition);
        }

        public override bool CheckValid()
        {
            if (!base.CheckValid())
                return false;

            HashSet<string> names = new HashSet<string>();
            bool res = true;
            foreach (FSMStateNode state in m_AllStates)
            {
                if (!names.Add(state.NickName))
                {
                    LogMgr.Instance.Error("Duplicate State Name: " + state.NickName);
                    res = false;
                }
            }

            return res;
        }
    }

    /// <summary>
    /// FSM state type
    /// </summary>
    public enum FSMStateType
    {
        /// <summary>
        /// Invalid
        /// </summary>
        Invalid,
        /// <summary>
        /// User created
        /// </summary>
        User,
        /// <summary>
        /// Special states like Entry, Exit, etc.
        /// </summary>
        Special,
    }
    /// <summary>
    /// Base class of FSM state node
    /// </summary>
    public abstract class FSMStateNode : FSMNode
    {
        public static readonly string TypeNormal = "Normal";
        public static readonly string TypeMeta = "Meta";
        public static readonly string TypeEntry = "Entry";
        public static readonly string TypeExit = "Exit";
        public static readonly string TypeAny = "Any";
        public static readonly string TypeUpper = "Upper";
        public static readonly HashSet<string> TypeSpecialStates = new HashSet<string> { TypeEntry, TypeExit, TypeAny, TypeUpper };

        public FSMStateType Type { get; set; } = FSMStateType.Invalid;
        public bool IsUserState { get { return Type == FSMStateType.User; } }
        /// <summary>
        /// The tree this state will execute
        /// </summary>
        public string Tree { get; set; } = string.Empty;
        public string Identification { get; set; } = string.Empty;

        private int m_SortIndex = 0;
        public int SortIndex
        {
            get { return m_SortIndex; }
            set
            {
                if (m_SortIndex >= 0)
                    m_SortIndex = value;
            }
        }

        public FSMStateNode() : base()
        {
        }

        public override string Note
        {
            get
            {
                return Tree;
            }
        }
        /// <summary>
        /// Load from file
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public bool Load(System.Xml.XmlNode data)
        {
            foreach (System.Xml.XmlAttribute attr in data.Attributes)
            {
                switch (attr.Name)
                {
                    case "Pos":
                        Geo.Pos = Point.Parse(attr.Value);
                        break;
                    case "Tree":
                        Tree = attr.Value;
                        break;
                    case "Name":
                        m_NickName = attr.Value;
                        break;
                    case "Comment":
                        Comment = attr.Value;
                        break;
                    case "Disabled":
                        Disabled = bool.Parse(attr.Value);
                        break;
                }
            }
            _OnLoaded();
            return true;
        }
        /// <summary>
        /// Save to file
        /// </summary>
        /// <param name="data"></param>
        public void Save(XmlElement data)
        {
            if (!string.IsNullOrEmpty(m_NickName))
                data.SetAttribute("Name", m_NickName);

            if (!string.IsNullOrEmpty(Tree))
                data.SetAttribute("Tree", Tree);

            Point intPos = new Point((int)Geo.Pos.X, (int)Geo.Pos.Y);
            data.SetAttribute("Pos", intPos.ToString());

            if (!string.IsNullOrEmpty(Comment))
            {
                data.SetAttribute("Comment", Comment);
            }

            if (SelfDisabled)
            {
                data.SetAttribute("Disabled", "true");
            }
        }
        /// <summary>
        /// Export to file
        /// </summary>
        /// <param name="data"></param>
        public void Export(XmlElement data)
        {
            if (!string.IsNullOrEmpty(m_NickName))
                data.SetAttribute("Name", m_NickName);

            if (!string.IsNullOrEmpty(Tree))
                data.SetAttribute("Tree", Tree);
        }

        protected virtual void _OnLoaded()
        {

        }

        public virtual void OnAddToMachine()
        {

        }

        protected override void _CreateRenderer()
        {
            m_Renderer = new FSMStateRenderer(this);
        }

        protected Connection _CreateConnection(Connector from, Connector to)
        {
            return new FSMConnection(from, to);
        }
    }
    /// <summary>
    /// FSM user created state node
    /// </summary>
    public class FSMUserStateNode : FSMStateNode
    {
        public FSMUserStateNode()
        {
            Type = FSMStateType.Invalid;
            Conns.Add(Connector.IdentifierParent, true, Connector.PosType.PARENT).ConnectionCreator = _CreateConnection;
            Conns.Add(Connector.IdentifierChildren, true, Connector.PosType.CHILDREN).ConnectionCreator = _CreateConnection;
        }
        public override bool CheckValid()
        {
            bool res = !string.IsNullOrEmpty(m_NickName);
            if (!res)
            {
                LogMgr.Instance.Error("Must have a NAME: " + Renderer.UITitle);
                return false;
            }
            if (!VariableCollection.IsValidVariableName(m_NickName))
            {
                LogMgr.Instance.Error("NAME format error: " + Renderer.UITitle);
                return false;
            }
            return res;
        }
    }
    /// <summary>
    /// A normal state
    /// </summary>
    public class FSMNormalStateNode : FSMUserStateNode
    {
        public FSMNormalStateNode() : base()
        {
            m_Name = "Normal";
            Type = FSMStateType.User;
        }
    }
    /// <summary>
    /// A state containing submachine
    /// </summary>
    public class FSMMetaStateNode : FSMUserStateNode
    {
        FSMMachineNode m_SubMachine;
        public FSMMachineNode SubMachine { get { return m_SubMachine; } }

        public FSMMetaStateNode() : base()
        {
            m_Name = "Meta";
            Type = FSMStateType.User;
        }

        public override void OnAddToMachine()
        {
            base.OnAddToMachine();
            m_SubMachine = FSMNodeMgr.Instance.CreateNode<FSMMachineNode>();
            m_SubMachine.MetaState = this;
            SubMachine.OwnerMachine = OwnerMachine;
            SubMachine.CreateUpper();
        }

        public override bool CheckValid()
        {
            bool res = base.CheckValid();

            res &= SubMachine.CheckValid();
            return res;
        }
    }
    /// <summary>
    /// Entry state
    /// </summary>
    public class FSMEntryStateNode : FSMStateNode
    {
        public FSMEntryStateNode()
        {
            m_Name = TypeEntry;
            Type = FSMStateType.Special;
            SortIndex = -4;

            Conns.Add(Connector.IdentifierChildren, true, Connector.PosType.CHILDREN).ConnectionCreator = _CreateConnection;
            Geo.Pos = new Point(100, 100);
        }
    }
    /// <summary>
    /// Exit state
    /// </summary>
    public class FSMExitStateNode : FSMStateNode
    {
        public FSMExitStateNode()
        {
            m_Name = TypeExit;
            Type = FSMStateType.Special;
            SortIndex = -3;

            Conns.Add(Connector.IdentifierParent, true, Connector.PosType.PARENT).ConnectionCreator = _CreateConnection;
            Geo.Pos = new Point(400, 400);
        }
    }
    /// <summary>
    /// Any state used for creating transition with any other states
    /// </summary>
    public class FSMAnyStateNode : FSMStateNode
    {
        public FSMAnyStateNode()
        {
            m_Name = TypeAny;
            Type = FSMStateType.Special;
            SortIndex = -2;

            Conns.Add(Connector.IdentifierChildren, true, Connector.PosType.CHILDREN).ConnectionCreator = _CreateConnection;
            Geo.Pos = new Point(250, 250);
        }
    }
    /// <summary>
    /// Upper state used for creating transition with states in upper layer machines
    /// </summary>
    public class FSMUpperStateNode : FSMStateNode
    {
        public FSMUpperStateNode()
        {
            m_Name = TypeUpper;
            Type = FSMStateType.Special;
            SortIndex = -1;

            Conns.Add(Connector.IdentifierParent, true, Connector.PosType.PARENT).ConnectionCreator = _CreateConnection;
            Conns.Add(Connector.IdentifierChildren, true, Connector.PosType.CHILDREN).ConnectionCreator = _CreateConnection;
            Geo.Pos = new Point(400, 100);
        }
    }
}
