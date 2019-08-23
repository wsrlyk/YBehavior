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
                    m_FSM.RootMachine.BuildConnections();
                    AddRenderers(m_FSM.RootMachine, false);
                }
                ///> TODO: comments should bind to submachines
                else if (chi.Name == "Comments")
                {
                    _LoadComments(chi);
                }
            }
            m_FSM.RemoveFlag(Graph.FLAG_LOADING);

            m_FSM.RefreshNodeUID();

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
                    if (!_LoadTrans(machine, chi))
                        return false;
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
                stateNode = FSMNodeMgr.Instance.CreateStateByName(type.Value);
            }
            else
            {
                stateNode = FSMNodeMgr.Instance.CreateStateByName(FSMStateNode.TypeNormal);
            }

            if (stateNode == null)
                return false;

            if (!stateNode.Load(data))
                return false;

            Utility.OperateNode(stateNode, m_Graph, false, NodeBase.OnAddToGraph);

            if (!machine.AddState(stateNode))
            {
                LogMgr.Instance.Error("Add state failed: " + stateNode.Name);
                return false;
            }

            if (stateNode is FSMMetaStateNode)
            {
                bool valid = false;
                foreach (XmlNode chi in data.ChildNodes)
                {
                    if (chi.Name == "Machine")
                    {
                        if (!_LoadMachine((stateNode as FSMMetaStateNode).SubMachine, chi))
                            return false;

                        valid = true;
                        break;
                    }
                }

                if (!valid)
                    return false;
            }

            return true;
        }

        protected bool _LoadTrans(FSMMachineNode machine, XmlNode data)
        {
            var attr = data.Attributes["Name"];
            string eventName = string.Empty;
            if (attr != null)
                eventName = attr.Value;

            attr = data.Attributes["From"];
            string from = string.Empty;
            if (attr != null)
                from = attr.Value;

            attr = data.Attributes["To"];
            string to = string.Empty;
            if (attr != null)
                to = attr.Value;

            if (!machine.TryAddTrans(eventName, from, to))
            {
                LogMgr.Instance.Error("Invalid trans: " + attr.ToString());
                return false;
            }

            return true;
        }

        public override void AddRenderers(NodeBase node, bool batchAdd, bool excludeRoot = false)
        {
            _AddRenderers(node as FSMNode);

            SelectionMgr.Instance.Clear();

            NodeList.Dispose();
            ConnectionList.Dispose();
        }

        void _AddRenderers(FSMNode node)
        {
            if (node is FSMMachineNode)
            {
                _AddMachineRenderers(node as FSMMachineNode);
            }
            else if (node is FSMStateNode)
            {
                _AddStateRenderer(node as FSMStateNode);
            }
        }

        void _AddMachineRenderers(FSMMachineNode node)
        {
            foreach (var state in node.States)
            {
                _AddStateRenderer(state);

                foreach (Connector ctr in state.Conns.ConnectorsList)
                {
                    foreach (Connection conn in ctr.Conns)
                    {
                        ConnectionList.DelayAdd(conn.Renderer);
                    }
                }
            }
        }

        void _AddStateRenderer(FSMStateNode node)
        {
            NodeList.DelayAdd(node.ForceGetRenderer);
        }
    }
}
