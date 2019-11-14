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
        public DelayableNotificationCollection<FSMMachineNode> StackMachines { get; } = new DelayableNotificationCollection<FSMMachineNode>();

        public FSMBench()
        {
            m_FSM = new FSM();
            m_Graph = m_FSM;
        }

        public override void InitEmpty()
        {
            Utility.OperateNode(m_FSM.RootMachine, m_Graph, false, NodeBase.OnAddToGraph);
            m_FSM.RefreshNodeUID();
            AddRenderers(m_FSM.RootMachine, true);
        }

        public override bool Load(XmlElement data)
        {
            CommandMgr.Blocked = true;
            m_FSM.SetFlag(Graph.FLAG_LOADING);
            foreach (XmlNode chi in data.ChildNodes)
            {
                if (chi.Name == "Machine")
                {
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

            if (!machine.PreLoad())
                return false;

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

            machine.Conns.Sort(Utility.SortByFSMNodeSortIndex);

            return true;
        }

        protected bool _LoadState(FSMMachineNode machine, XmlNode data)
        {
            FSMStateNode stateNode = null;
            bool bNewState = true;
            var type = data.Attributes["Type"];
            if (type != null)
            {
                if (FSMStateNode.TypeSpecialStates.Contains(type.Value))
                {
                    stateNode = machine.FindState(type.Value);
                    bNewState = false;
                }
                else
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

            if (bNewState)
            {
                Utility.OperateNode(stateNode, m_Graph, false, NodeBase.OnAddToGraph);

                if (!machine.AddState(stateNode))
                {
                    LogMgr.Instance.Error("Add state failed: " + stateNode.Name);
                    return false;
                }
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
            var attr = data.Attributes["From"];
            string from = string.Empty;
            if (attr != null)
                from = attr.Value;

            attr = data.Attributes["To"];
            string to = string.Empty;
            if (attr != null)
                to = attr.Value;

            List<string> events = new List<string>();
            foreach (XmlNode chi in data.ChildNodes)
            {
                events.Add(chi.Name);
            }

            if (!machine.TryAddTrans(from, to, events))
            {
                LogMgr.Instance.Error("Invalid trans: " + attr.ToString());
                return false;
            }

            return true;
        }

        void _RefreshStackMachines(FSMNode node)
        {
            using (var v = StackMachines.Delay())
            {
                StackMachines.Clear();
                if (node != null)
                {
                    if (node is FSMMachineNode)
                    {
                        FSMMachineNode cur = node as FSMMachineNode;
                        while (cur != null)
                        {
                            StackMachines.Add(cur);
                            cur = cur.MetaState?.OwnerMachine;
                        }
                    }
                }
            }
        }

        public override void Save(XmlElement data, XmlDocument xmlDoc)
        {
            CommandMgr.Blocked = true;
            _SaveMachine(m_FSM.RootMachine, data, xmlDoc, false);
            CommandMgr.Blocked = false;

            CommandMgr.Dirty = false;
            m_ExportFileHash = 0;

            OnPropertyChanged("DisplayName");
        }

        void _SaveMachine(FSMMachineNode machine, XmlElement data, XmlDocument xmlDoc, bool bExport)
        {
            XmlElement nodeEl = xmlDoc.CreateElement("Machine");
            data.AppendChild(nodeEl);

            if (machine.DefaultState != null)
                nodeEl.SetAttribute("Default", machine.DefaultState.NickName);

            foreach (FSMStateNode state in machine.States)
            {
                _SaveState(state, nodeEl, xmlDoc, bExport);
            }

            if (machine is FSMRootMachineNode)
            {
                foreach (TransitionResult trans in (machine as FSMRootMachineNode).Transition)
                {
                    _SaveTrans(trans, nodeEl, xmlDoc);
                }
            }
        }

        void _SaveState(FSMStateNode state, XmlElement data, XmlDocument xmlDoc, bool bExport)
        {
            XmlElement nodeEl = xmlDoc.CreateElement("State");
            data.AppendChild(nodeEl);

            if (!(state is FSMNormalStateNode))
                nodeEl.SetAttribute("Type", state.Name);

            if (bExport)
                state.Export(nodeEl);
            else
                state.Save(nodeEl);

            if (state is FSMMetaStateNode)
            {
                _SaveMachine((state as FSMMetaStateNode).SubMachine, nodeEl, xmlDoc, bExport);
            }
        }

        void _SaveTrans(TransitionResult trans, XmlElement data, XmlDocument xmlDoc)
        {
            XmlElement nodeEl = xmlDoc.CreateElement("Trans");
            data.AppendChild(nodeEl);

            if (trans.Key.FromState != null)
                nodeEl.SetAttribute("From", trans.Key.FromState.NickName);

            if (trans.Key.ToState != null)
                nodeEl.SetAttribute("To", trans.Key.ToState.NickName);

            foreach (var e in trans.Value)
            {
                XmlElement el = xmlDoc.CreateElement(e.Event.Event);
                nodeEl.AppendChild(el);
            }
        }

        public override void Export(XmlElement data, XmlDocument xmlDoc)
        {
            CommandMgr.Blocked = true;
            _SaveMachine(m_FSM.RootMachine, data, xmlDoc, true);
            CommandMgr.Blocked = false;
        }

        public override void AddRenderers(NodeBase node, bool batchAdd, bool excludeRoot = false)
        {
            NodeList.Clear();
            ConnectionList.Clear();

            _AddRenderers(node as FSMNode);

            SelectionMgr.Instance.Clear();

            NodeList.Dispose();
            ConnectionList.Dispose();

            _RefreshStackMachines(node as FSMNode);
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

        public override void RemoveRenderers(NodeBase node, bool excludeRoot = false)
        {
            foreach (Connector ctr in node.Conns.ConnectorsList)
            {
                foreach (Connection conn in ctr.Conns)
                {
                    ConnectionList.Remove(conn.Renderer);
                }
            }

            NodeList.Remove(node.Renderer);
        }

        public override void DisconnectNodes(Connection.FromTo connection)
        {
            FSMConnection conn = connection.From.FindConnection(connection) as FSMConnection;
            if (conn == null)
                return;

            while(conn.Trans.Count > 0)
            {
                Disconnect(conn, conn.Trans[0]);
            }

            DisconnectNodeCommand disconnectNodeCommand = new DisconnectNodeCommand
            {
                Conn = connection,
            };
            PushDoneCommand(disconnectNodeCommand);
        }

        public void Disconnect(Connection.FromTo fromto, TransitionResult trans)
        {
            FSMConnection conn = fromto.From.FindConnection(fromto) as FSMConnection;
            if (conn == null)
                return;
            Disconnect(conn, trans);
        }

        void Disconnect(FSMConnection conn, TransitionResult trans)
        {
            ConnectionRenderer connectionRenderer = conn.Renderer;
            m_FSM.RootMachine.RemoveTrans(trans);
            if (conn.Trans.Count == 0)
            {
                if (connectionRenderer != null)
                    ConnectionList.Remove(connectionRenderer);

            }
        }

        public override void ConnectNodes(Connector from, Connector to)
        {
            if (from == null || to == null || from.GetDir != Connector.Dir.OUT || to.GetDir != Connector.Dir.IN)
                return;
            var res = m_FSM.RootMachine.MakeTrans(from.Owner as FSMStateNode, to.Owner as FSMStateNode);
            if (res.Route != null)
            {
                foreach (var p in res.Route)
                {
                    if (p.Key != from.Owner)
                        continue;

                    Connection.FromTo fromto = new Connection.FromTo
                    {
                        From = from,
                        To = p.Value.Conns.ParentConnector
                    };
                    FSMConnection conn = from.FindConnection(fromto) as FSMConnection;
                    ///> There may be multiple trans in one connection. We only add the renderer when the
                    ///  first trans is built
                    if (conn != null && conn.Trans.Count == 1)
                    {
                        ConnectionRenderer connRenderer = conn.Renderer;
                        if (connRenderer != null)
                            ConnectionList.Add(connRenderer);
                    }

                    break;
                }
            }

            ConnectNodeCommand connectNodeCommand = new ConnectNodeCommand
            {
                Conn = new Connection.FromTo{ From = from, To = to },
            };
            PushDoneCommand(connectNodeCommand);
        }

        public void SetDefault(FSMStateNode state)
        {
            if (state == null)
                return;
            FSMConnection oldConn = null;
            FSMConnection newConn = null;

            FSMMachineNode machine = state.OwnerMachine;
            if (machine.SetDefault(state, ref oldConn, ref newConn))
            {
                if (oldConn.Trans.Count == 0)
                    ConnectionList.Remove(oldConn.Renderer);
                if (newConn.Trans.Count == 1)
                    ConnectionList.Add(newConn.Renderer);
            }
        }

        public override void AddNode(NodeBase node)
        {
            FSMStateNode state = node as FSMStateNode;
            FSMMachineNode curMachine = this.StackMachines[0];
            Utility.OperateNode(node, m_Graph, true, NodeBase.OnAddToGraph);

            if (!curMachine.AddState(state))
                return;

            _AddStateRenderer(state);
            SelectionMgr.Instance.Clear();

            NodeList.Dispose();

            m_FSM.RefreshNodeUID();


            AddNodeCommand addNodeCommand = new AddNodeCommand()
            {
                Node = node
            };
            PushDoneCommand(addNodeCommand);
        }

        public override void RemoveNode(NodeBase node)
        {
            RemoveRenderers(node);
            FSMStateNode state = node as FSMStateNode;
            state.OwnerMachine.RemoveState(state);

            RemoveNodeCommand removeNodeCommand = new RemoveNodeCommand()
            {
                Node = node
            };
            PushDoneCommand(removeNodeCommand);
        }

        public override bool CheckError()
        {
            return _CheckError(m_FSM.RootMachine);
        }

        private bool _CheckError(FSMMachineNode machine)
        {
            return machine.CheckValid();
        }
    }
}
