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
        public List<FSMStateNode> StateList { get { return m_StateList; } }
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

        Transition m_LocalTransition = new Transition();
        public Transition LocalTransition { get { return m_LocalTransition; } }

        public FSMMachineNode()
        {
        }

        public bool PostLoad(XmlNode data)
        {
            var attr = data.Attributes["Default"];
            if (attr != null)
            {
                FSMStateNode state = FindState(attr.Value);
                if (state!= null)
                {
                    SetDefault(state);
                }
                else
                {
                    LogMgr.Instance.Error("Cant find state as default: " + attr.Value);
                    return false;
                }
            }

            _ForceAddSpecialState(ref m_Entry, FSMStateNode.TypeEntry);
            _ForceAddSpecialState(ref m_Exit, FSMStateNode.TypeExit);
            _ForceAddSpecialState(ref m_Any, FSMStateNode.TypeAny);

            if (OwnerMachine != null)
                _ForceAddSpecialState(ref m_Upper, FSMStateNode.TypeUpper);

            return true;
        }

        FSMStateNode FindState(string name)
        {
            foreach (var state in m_States)
            {
                if (state.NickName == name)
                    return state;
            }
            return null;
        }

        public bool AddState(FSMStateNode state)
        {
            state.OwnerMachine = this;
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

        public void SetDefault(FSMStateNode state)
        {
            ///> TODO

            m_Default = state;
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

        public bool TryAddTrans(string eventName, string from, string to)
        {
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
                LogMgr.Instance.Error("To state is necessary.");
                return false;
            }

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

            if (!RootMachine.Transition.Insert(fromState, eventName, toState))
                return false;

            ///> TODO: build connection
            
            return true;
        }
    }

    public class FSMRootMachineNode : FSMMachineNode
    {
        List<FSMStateNode> m_AllStates = new List<FSMStateNode>();
        Transition m_Transition = new Transition();
        public Transition Transition { get { return m_Transition; } }

        public override void CreateBase()
        {
            base.CreateBase();
            OwnerMachine = null;
        }

        public bool AddGlobalState(FSMStateNode state)
        {
            if (state == null || string.IsNullOrEmpty(state.NickName))
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
                FSMStateNode toState = t.Value.ToState;
                string eventName = t.Key.Trans.Event;

                if (toState == null)
                {
                    LogMgr.Instance.Error("ToState is null");
                    return;
                }

                ///> fromState == null ->  AnyState=>ToState
                if (fromState == null)
                {
                    fromState = toState.OwnerMachine.AnyState;
                    _BuildConnection(fromState, toState, t);
                }
                ///> In the same machine, simplest situation, nothing to do
                else if (fromState.OwnerMachine == toState.OwnerMachine)
                {
                    _BuildConnection(fromState, toState, t);
                }
                ///> Find their ancester
                else
                {
                    FSMMachineNode toppestLevelChild = null;
                    FSMMachineNode ancestorMachine = Utility.FindAncestor(fromState.OwnerMachine, toState.OwnerMachine, ref toppestLevelChild);

                    ///> fromState is the parent of toState 
                    ///>    ---> fromState=>toppestLevelChild
                    ///>    ---> UpperState=>toState
                    if (ancestorMachine == fromState.OwnerMachine)
                    {
                        if (toppestLevelChild == null 
                            || toppestLevelChild.MetaState == null
                            || toppestLevelChild.MetaState.OwnerMachine != fromState.OwnerMachine)
                        {
                            LogMgr.Instance.Error("Something error when find ancestor");
                            return;
                        }
                        _BuildConnection(fromState, toppestLevelChild.MetaState, t);
                        _BuildConnection(toState.OwnerMachine.UpperState, toState, t);
                    }
                    ///> toState is the parent of fromState 
                    ///>    ---> fromState=>UpperState
                    ///>    ---> toppestLevelChild=>ToState
                    else if (ancestorMachine == toState.OwnerMachine)
                    {
                        if (toppestLevelChild == null
                            || toppestLevelChild.MetaState == null
                            || toppestLevelChild.MetaState.OwnerMachine != toState.OwnerMachine)
                        {
                            LogMgr.Instance.Error("Something error when find ancestor");
                            return;
                        }
                        _BuildConnection(fromState, fromState.OwnerMachine.UpperState, t);
                        _BuildConnection(toppestLevelChild.MetaState, toState, t);
                    }
                }
            }
        }

        void _BuildConnection(FSMStateNode fromState, FSMStateNode toState, TransitionResult trans)
        {
            if (fromState == null || toState == null || fromState.OwnerMachine != toState.OwnerMachine)
                return;

            fromState.Conns.Connect(toState, Connector.IdentifierChildren);
        }
    }


    public enum FSMStateType
    {
        Invalid,
        Normal,
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

        public FSMStateType Type { get; set; } = FSMStateType.Invalid;
        public bool IsUserState { get { return Type == FSMStateType.Normal; } }

        public string Tree { get; set; } = string.Empty;
        public string Identification { get; set; } = string.Empty;

        public FSMStateNode() : base()
        {
            Conns.Add(Connector.IdentifierParent, true);
            Conns.Add(Connector.IdentifierChildren, true);
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
                    case "Identification":
                        Identification = attr.Value;
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
    }

    public class FSMNormalStateNode : FSMStateNode
    {
        public FSMNormalStateNode()
        {
            m_Name = "Normal";
            Type = FSMStateType.Normal;

            Conns.Add(Connector.IdentifierParent, true);
            Conns.Add(Connector.IdentifierChildren, true);
        }
    }

    public class FSMMetaStateNode : FSMStateNode
    {
        FSMMachineNode m_SubMachine;
        public FSMMachineNode SubMachine { get { return m_SubMachine; } }

        public FSMMetaStateNode()
        {
            m_Name = "Meta";
            Type = FSMStateType.Normal;

            Conns.Add(Connector.IdentifierParent, true);
            Conns.Add(Connector.IdentifierChildren, true);
        }

        public override void OnAddToMachine()
        {
            base.OnAddToMachine();
            m_SubMachine = FSMNodeMgr.Instance.CreateNode<FSMMachineNode>();
            m_SubMachine.MetaState = this;
            SubMachine.OwnerMachine = OwnerMachine;
        }
    }

    public class FSMEntryStateNode : FSMStateNode
    {
        public FSMEntryStateNode()
        {
            m_Name = TypeEntry;
            Type = FSMStateType.Special;

            Conns.Add(Connector.IdentifierChildren, false);
        }
    }

    public class FSMExitStateNode : FSMStateNode
    {
        public FSMExitStateNode()
        {
            m_Name = TypeExit;
            Type = FSMStateType.Special;

            Conns.Add(Connector.IdentifierParent, true);
        }
    }

    public class FSMAnyStateNode : FSMStateNode
    {
        public FSMAnyStateNode()
        {
            m_Name = TypeAny;
            Type = FSMStateType.Special;

            Conns.Add(Connector.IdentifierChildren, true);
        }
    }

    public class FSMUpperStateNode : FSMStateNode
    {
        public FSMUpperStateNode()
        {
            m_Name = TypeUpper;
            Type = FSMStateType.Special;

            Conns.Add(Connector.IdentifierParent, true);
        }
    }
}
