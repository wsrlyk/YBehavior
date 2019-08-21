using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace YBehavior.Editor.Core.New
{
    public class FSMBench : WorkBench
    {
        FSM m_FSM;

        public FSMBench()
        {
            m_FSM = new FSM();
            m_Graph = m_FSM;
        }

        public override bool Load(XmlElement data)
        {
            CommandMgr.Blocked = true;
            m_FSM.SetFlag(Graph.FLAG_LOADING);
            foreach (XmlNode chi in data.ChildNodes)
            {
                if (chi.Name == "Machine")
                {
                    m_FSM.CreateRoot();
                    _LoadMachine(m_FSM.RootMachine, chi);
                    AddRenderers(m_FSM.RootMachine, false);
                }
                ///> TODO: comments should bind to submachines
                else if (chi.Name == "Comments")
                {
                    _LoadComments(chi);
                }
            }

            m_FSM.RemoveFlag(Graph.FLAG_LOADING);

            CommandMgr.Blocked = false;
            return true;
        }

        protected bool _LoadMachine(FSMMachineNode machine, XmlNode data)
        {
            Utility.OperateNode(machine, m_Graph, false, NodeBase.OnAddToGraph);

            foreach (XmlNode chi in data.ChildNodes)
            {
                if (chi.Name == "State")
                {
                    if (!_LoadState(machine, chi))
                        return false;
                }
                else if (chi.Name == "Trans")
                {

                }
            }

            if (!machine.PostLoad(data))
                return false;
            return true;
        }

        protected bool _LoadState(FSMMachineNode machine, XmlNode data)
        {
            FSMStateNode stateNode = null;
            var type = data.Attributes["Type"];
            if (type != null)
            {
                if (type.Value == "Meta")
                {
                    foreach (XmlNode chi in data.ChildNodes)
                    {
                        if (chi.Name == "Machine")
                        {
                            FSMMetaStateNode metaNode = FSMNodeMgr.Instance.CreateNode<FSMMetaStateNode>();
                            stateNode = metaNode;

                            break;
                        }
                    }
                }
            }
            if (stateNode == null)
                stateNode = FSMNodeMgr.Instance.CreateNode<FSMStateNode>();
            stateNode.Load(data);
            Utility.OperateNode(stateNode, m_Graph, false, NodeBase.OnAddToGraph);

            machine.AddState(stateNode);

            if (stateNode.Type == FSMStateType.Meta)
            {
                foreach (XmlNode chi in data.ChildNodes)
                {
                    if (chi.Name == "Machine")
                    {
                        if (!_LoadMachine((stateNode as FSMMetaStateNode).SubMachine, chi))
                            return false;

                        break;
                    }
                }
            }

            return true;
        }
    }
}
