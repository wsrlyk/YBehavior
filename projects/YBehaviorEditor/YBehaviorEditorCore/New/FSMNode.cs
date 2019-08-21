using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace YBehavior.Editor.Core.New
{
    public class FSMNodeMgr : Singleton<FSMNodeMgr>
    {
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
        List<FSMStateNode> m_States = new List<FSMStateNode>();
        FSMStateNode m_Entry;
        FSMStateNode m_Exit;
        FSMStateNode m_Default;
        FSMStateNode m_Any;

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

            return true;
        }

        FSMStateNode FindState(string name)
        {
            foreach (var state in m_States)
            {
                if (state.Name == name)
                    return state;
            }
            return null;
        }
        public void AddState(FSMStateNode state)
        {
            state.OwnerMachine = this;
            m_States.Add(state);

            switch (state.Type)
            {
                case FSMStateType.Entry:
                    _ProcessSpecialState(ref m_Entry, state);
                    break;
                case FSMStateType.Exit:
                    _ProcessSpecialState(ref m_Exit, state);
                    break;
                case FSMStateType.Any:
                    _ProcessSpecialState(ref m_Any, state);
                    break;
                default:
                    break;
            }
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
    }

    public class FSMRootMachineNode : FSMMachineNode
    {
        List<FSMStateNode> m_AllStates = new List<FSMStateNode>();

        public override void CreateBase()
        {
            base.CreateBase();
            OwnerMachine = null;
        }

        public void AddGlobalState(FSMStateNode state)
        {
            m_AllStates.Add(state);
        }
    }


    public enum FSMStateType
    {
        Entry,
        Exit,
        Meta,
        Any,
        Default,
    }

    public class FSMStateNode : FSMNode
    {
        public FSMStateType Type { get; set; } = FSMStateType.Default;

        public string Tree { get; set; } = string.Empty;
        public string Identification { get; set; } = string.Empty;

        public bool Load(XmlNode data)
        {
            var attr = data.Attributes["Tree"];
            if (attr != null)
                Tree = attr.Value;

            attr = data.Attributes["Identification"];
            if (attr != null)
                Identification = attr.Value;

            attr = data.Attributes["Name"];
            if (attr != null)
                m_Name = attr.Value;


            attr = data.Attributes["Type"];
            if (attr != null)
            {
                var o = Enum.Parse(typeof(FSMStateType), attr.Value);
                if (o != null)
                    Type = (FSMStateType)o;
                else
                {
                    LogMgr.Instance.Error("Invalid type " + attr.Value);
                    return false;
                }
            }

            return true;
        }

        public virtual void OnAddToMachine()
        {

        }
    }

    public class FSMMetaStateNode : FSMStateNode
    {
        FSMMachineNode m_SubMachine = new FSMMachineNode();
        public FSMMachineNode SubMachine { get { return m_SubMachine; } }

        public FSMMetaStateNode()
        {
            m_SubMachine = FSMNodeMgr.Instance.CreateNode<FSMMachineNode>();
            Type = FSMStateType.Meta;
        }
    }
}
