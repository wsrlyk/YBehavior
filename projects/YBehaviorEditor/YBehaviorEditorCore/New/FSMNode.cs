﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Xml;

namespace YBehavior.Editor.Core.New
{
    public class FSMNodeMgr : Singleton<FSMNodeMgr>
    {
        List<FSMStateNode> m_StateList = new List<FSMStateNode>();
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
                Console.WriteLine(type);
            }
        }

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

        public T CreateNode<T>() where T: FSMNode, new()
        {
            T t = new T();
            t.CreateBase();
            return t;
        }
    }

    public class FSMNodeWrapper : NodeWrapper
    {
        public FSM FSM { get { return Graph as FSM; } }
        FSMRootMachineNode m_RootMachine;
        public FSMRootMachineNode RootMachine { get { return m_RootMachine; } }

        FSMMachineNode m_OwnerMachine;
        public FSMMachineNode OwnerMachine { get { return m_OwnerMachine; } }

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

    public class FSMNode : NodeBase
    {
        protected override void _CreateWrapper()
        {
            m_Wrapper = new FSMNodeWrapper();
        }

        public FSMRootMachineNode RootMachine
        {
            get { return (m_Wrapper as FSMNodeWrapper).RootMachine; }
        }

        public FSMMachineNode OwnerMachine
        {
            get { return (m_Wrapper as FSMNodeWrapper).OwnerMachine; }
            set { (m_Wrapper as FSMNodeWrapper).SetOwner(value); }
        }
    }
    public class FSMMachineNode : FSMNode
    {
        public FSMMetaStateNode MetaState { get; set; }

        List<FSMStateNode> m_States = new List<FSMStateNode>();
        FSMStateNode m_Entry;
        FSMStateNode m_Exit;
        FSMStateNode m_Default;
        FSMStateNode m_Any;
        FSMStateNode m_Upper;

        public List<FSMStateNode> States { get { return m_States; } }
        public FSMStateNode EntryState { get { return m_Entry; } }
        public FSMStateNode ExitState { get { return m_Exit; } }
        public FSMStateNode DefaultState { get { return m_Default; } }
        public FSMStateNode AnyState { get { return m_Any; } }
        public FSMStateNode UpperState { get { return m_Upper; } }

        Transitions m_LocalTransition = new Transitions();
        public Transitions LocalTransition { get { return m_LocalTransition; } }

        public uint Level { get; set; } = 0;

        public FSMMachineNode()
        {
        }

        public override void CreateBase()
        {
            base.CreateBase();
            CreateEmpty();
        }

        public void CreateEmpty()
        {
            _ForceAddSpecialState(ref m_Entry, FSMStateNode.TypeEntry);
            _ForceAddSpecialState(ref m_Exit, FSMStateNode.TypeExit);
            _ForceAddSpecialState(ref m_Any, FSMStateNode.TypeAny);
        }

        public void CreateUpper()
        {
            if (OwnerMachine != null)
                _ForceAddSpecialState(ref m_Upper, FSMStateNode.TypeUpper);
        }

        protected override void _CreateRenderer()
        {
            m_Renderer = new FSMMachineRenderer(this);
        }

        public bool PreLoad()
        {
            return true;
        }

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

            return true;
        }

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
        public bool RemoveState(FSMStateNode state)
        {
            if (state.Type == FSMStateType.Special)
                return false;

            m_States.Remove(state);
            if (state.IsUserState)
                return RootMachine.RemoveGlobalState(state);

            return true;
        }
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

        public bool SetDefault(FSMStateNode state, ref FSMConnection oldConn, ref FSMConnection newConn)
        {
            if (state == null)
                return false;
            if (state.OwnerMachine != this)
                return false;
            if (state.Type == FSMStateType.Special)
                return false;

            if (m_Default != null)
            {
                Connection.FromTo fromto = new Connection.FromTo
                {
                    From = EntryState.Conns.GetConnector(Connector.IdentifierChildren),
                    To = m_Default.Conns.ParentConnector
                };

                FSMConnection conn = fromto.From.FindConnection(fromto) as FSMConnection;
                if (conn == null)
                {
                    LogMgr.Instance.Error("There's a default state but cant find the connection");
                    return false;
                }
                TransitionResult defaultTrans = null;
                foreach (var trans in conn.Trans)
                {
                    if (trans.Type == TransitionResultType.Default)
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
            }
            /// new default
            {
                Connector parent;
                Connector child;

                FSMConnection conn = Connector.TryConnect(EntryState.Conns.GetConnector(Connector.IdentifierChildren), state.Conns.ParentConnector, out parent, out child) as FSMConnection;
                if (conn != null)
                {
                    TransitionResult res = new TransitionResult(new TransitionMapKey(EntryState, state));
                    res.Type = TransitionResultType.Default;
                    conn.Trans.Add(res);

                }
                else
                {
                    return false;
                }
                newConn = conn;
            }

            m_Default = state;
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
            Utility.OperateNode(state, Graph, false, NodeBase.OnAddToGraph);

            AddState(state);
        }

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
            else
            {
                if (fromState == null)
                {
                    LogMgr.Instance.Error("Cant trans from AnyState to ExitState");
                    return false;
                }
                ///> 'to' is null means trans from 'from' to 'exit'
                toState = this.ExitState;
                if (toState == null)
                {
                    LogMgr.Instance.Error("ExitState not exists.");
                    return false;
                }
            }

            if (RootMachine.Transition.Insert(fromState, toState, events) == null)
                return false;

            ///> TODO: build connection
            
            return true;
        }

        public override bool CheckValid()
        {
            bool res = true;
            foreach (FSMStateNode state in m_States)
            {
                res &= state.CheckValid();
            }

            return res;
        }
    }

    public class FSMRootMachineNode : FSMMachineNode
    {
        List<FSMStateNode> m_AllStates = new List<FSMStateNode>();
        Transitions m_Transition = new Transitions();
        public Transitions Transition { get { return m_Transition; } }

        public override void CreateBase()
        {
            base.CreateBase();
            OwnerMachine = null;
        }

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

        public FSMStateNode FindGloablState(string name)
        {
            //FSMStateNode state;
            foreach (var node in m_AllStates)
            {
                if (node.NickName == name)
                    return node;
            }
            //m_AllStates.TryGetValue(name, out state);
            return null;
            //return state;
        }

        public void BuildConnections()
        {
            foreach (TransitionResult t in m_Transition)
            {
                FSMStateNode fromState = t.Key.FromState;
                FSMStateNode toState = t.Key.ToState;

                var res = Utility.FindTransRoute(fromState, toState);
                foreach(var p in res.Route)
                {
                    _BuildConnection(p.Key, p.Value, t);
                }
            }
        }

        void _BuildConnection(FSMStateNode fromState, FSMStateNode toState, TransitionResult trans)
        {
            if (fromState == null || toState == null || fromState.OwnerMachine != toState.OwnerMachine)
                return;

            FSMConnection conn = fromState.Conns.Connect(toState, Connector.IdentifierChildren) as FSMConnection;
            conn.Trans.Add(trans);
        }

        public void RemoveTrans(TransitionResult t)
        {
            FSMStateNode fromState = t.Key.FromState;
            FSMStateNode toState = t.Key.ToState;

            var res = Utility.FindTransRoute(fromState, toState);
            foreach (var p in res.Route)
            {
                _RemoveConnection(p.Key, p.Value, t);
            }

            Transition.Remove(t);
        }

        void _RemoveConnection(FSMStateNode fromState, FSMStateNode toState, TransitionResult trans)
        {
            if (fromState == null || toState == null || fromState.OwnerMachine != toState.OwnerMachine)
                return;
            Connection.FromTo fromto = new Connection.FromTo
            {
                From = fromState.Conns.GetConnector(Connector.IdentifierChildren),
                To = toState.Conns.ParentConnector
            };

            FSMConnection conn = fromto.From.FindConnection(fromto) as FSMConnection;
            if (conn == null)
                return;

            conn.Trans.Remove(trans);
            if (conn.Trans.Count == 0)
                Connector.TryDisconnect(fromto);
        }

        public TransRoute MakeTrans(FSMStateNode fromState, FSMStateNode toState)
        {
            TransitionResult trans = Transition.Insert(fromState, toState);
            if (trans == null)
                return new TransRoute();

            ///> FromState may change if it's an AnyState
            var res = Utility.FindTransRoute(trans.Key.FromState, trans.Key.ToState);
            foreach (var p in res.Route)
            {
                _MakeTrans(p.Key, p.Value, trans);
            }
            return res;
        }

        void _MakeTrans(FSMStateNode fromState, FSMStateNode toState, TransitionResult t)
        {
            Connector parent;
            Connector child;
            FSMConnection conn = Connector.TryConnect(fromState.Conns.GetConnector(Connector.IdentifierChildren), toState.Conns.ParentConnector, out parent, out child) as FSMConnection;
            if (conn != null)
                conn.Trans.Add(t);
        }
    }


    public enum FSMStateType
    {
        Invalid,
        User,
        Special,
    }

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
                    case "DebugPoint":
                        DebugPointInfo.HitCount = int.Parse(attr.Value);
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

        public void Save(XmlElement data)
        {
            if (!string.IsNullOrEmpty(m_NickName))
                data.SetAttribute("Name", m_NickName);

            if (!string.IsNullOrEmpty(Tree))
                data.SetAttribute("Tree", Tree);

            Point intPos = new Point((int)Geo.Pos.X, (int)Geo.Pos.Y);
            data.SetAttribute("Pos", intPos.ToString());

            if (!DebugPointInfo.NoDebugPoint)
                data.SetAttribute("DebugPoint", DebugPointInfo.HitCount.ToString());

            if (!string.IsNullOrEmpty(Comment))
            {
                data.SetAttribute("Comment", Comment);
            }

            if (SelfDisabled)
            {
                data.SetAttribute("Disabled", "true");
            }
        }

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

    public class FSMUserStateNode : FSMStateNode
    {
        public FSMUserStateNode()
        {
            Type = FSMStateType.Invalid;
            Conns.Add(Connector.IdentifierParent, true).ConnectionCreator = _CreateConnection;
            Conns.Add(Connector.IdentifierChildren, true).ConnectionCreator = _CreateConnection;
        }
        public override bool CheckValid()
        {
            bool res = !string.IsNullOrEmpty(m_NickName);
            if (!res)
            {
                LogMgr.Instance.Log("Must have a NAME: " + Renderer.UITitle);
            }
            return res;
        }
    }
    public class FSMNormalStateNode : FSMUserStateNode
    {
        public FSMNormalStateNode() : base()
        {
            m_Name = "Normal";
            Type = FSMStateType.User;
        }
    }

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

    public class FSMEntryStateNode : FSMStateNode
    {
        public FSMEntryStateNode()
        {
            m_Name = TypeEntry;
            Type = FSMStateType.Special;
            SortIndex = -4;

            Conns.Add(Connector.IdentifierChildren, false).ConnectionCreator = _CreateConnection;
            Geo.Pos = new Point(100, 100);
        }
    }

    public class FSMExitStateNode : FSMStateNode
    {
        public FSMExitStateNode()
        {
            m_Name = TypeExit;
            Type = FSMStateType.Special;
            SortIndex = -3;

            Conns.Add(Connector.IdentifierParent, true).ConnectionCreator = _CreateConnection;
            Geo.Pos = new Point(400, 400);
        }
    }

    public class FSMAnyStateNode : FSMStateNode
    {
        public FSMAnyStateNode()
        {
            m_Name = TypeAny;
            Type = FSMStateType.Special;
            SortIndex = -2;

            Conns.Add(Connector.IdentifierChildren, true).ConnectionCreator = _CreateConnection;
            Geo.Pos = new Point(250, 250);
        }
    }

    public class FSMUpperStateNode : FSMStateNode
    {
        public FSMUpperStateNode()
        {
            m_Name = TypeUpper;
            Type = FSMStateType.Special;
            SortIndex = -1;

            Conns.Add(Connector.IdentifierParent, true).ConnectionCreator = _CreateConnection;
            Conns.Add(Connector.IdentifierChildren, true).ConnectionCreator = _CreateConnection;
            Geo.Pos = new Point(400, 100);
        }
    }
}
