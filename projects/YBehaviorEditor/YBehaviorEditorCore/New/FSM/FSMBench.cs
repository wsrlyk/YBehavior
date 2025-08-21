using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace YBehavior.Editor.Core.New
{
    /// <summary>
    /// Operating an FSM
    /// </summary>
    public class FSMBench : WorkBench
    {
        FSM m_FSM;
        public FSM FSM { get { return m_FSM; } }
        /// <summary>
        /// The layers of fsm
        /// </summary>
        public DelayableNotificationCollection<FSMMachineNode> StackMachines { get; } = new DelayableNotificationCollection<FSMMachineNode>();
        /// <summary>
        /// The top layer
        /// </summary>
        public FSMMachineNode CurMachine { get { return StackMachines.Count > 0 ? StackMachines[0] : null; } }

        protected override FileType FileType => FileType.FSM;
        public FSMBench()
        {
            m_FSM = new FSM();
            m_Graph = m_FSM;
        }

        public override void InitEmpty()
        {
            NodeBase.OnAddToGraph(m_FSM.RootMachine, m_Graph);
            m_FSM.RefreshNodeUID(0);
            AddRenderers(m_FSM.RootMachine, true);
        }

        void _LoadSuo()
        {
            string fileName = this.FileInfo.RelativeName;
            var map = Config.Instance.Suo.GetSuo(fileName);
            if (map != null)
            {
                Action<FSMStateNode> action = (FSMStateNode node) =>
                {
                    node.DebugPointInfo.HitCount = map.GetDebugPoint(node.UID);
                };
                Utility.ForEachFSMState(m_FSM, action);
            }
        }

        public override bool Load(XmlElement data, bool bRendering)
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

            m_FSM.RefreshNodeUID(0);

            CommandMgr.Blocked = false;

            _LoadSuo();
            return true;
        }

        protected bool _LoadMachine(FSMMachineNode machine, XmlNode data)
        {
            NodeBase.OnAddToGraph(machine, m_Graph);
            //Utility.OperateNode(machine, m_Graph, false, NodeBase.OnAddToGraph);

            //if (!machine.PreLoad())
            //    return false;

            foreach (XmlNode chi in data.ChildNodes)
            {
                switch (chi.Name)
                {
                    case "State":
                        {
                            if (!_LoadState(machine, chi))
                                return false;
                            break;
                        }
                    case "Trans":
                        {
                            if (!_LoadTrans(machine, chi))
                                return false;
                            break;
                        }
                    case "EntryTrans":
                        {
                            if (!_LoadEntryTrans(machine, chi))
                                return false;
                            break;
                        }
                    case "ExitTrans":
                        {
                            if (!_LoadEntryTrans(machine, chi))
                                return false;
                            break;
                        }
                    default:
                        break;
                }
            }

            if (!machine.PostLoad(data))
                return false;
            machine.States.Sort(Utility.SortByFSMNodeSortIndex);

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
                NodeBase.OnAddToGraph(stateNode, m_Graph);
                //Utility.OperateNode(stateNode, m_Graph, false, NodeBase.OnAddToGraph);

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
            TransitionType t = TransitionType.Normal;
            var attr = data.Attributes["Type"];
            if (attr != null)
            {
                if (attr.Value == "Entry")
                    t = TransitionType.Entry;
                else if (attr.Value == "Exit")
                    t = TransitionType.Exit;
            }

            attr = data.Attributes["From"];
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

            switch (t)
            {
                case TransitionType.Entry:
                    if (!machine.TryAddEntryTrans(to, events))
                    {
                        LogMgr.Instance.Error("Invalid entry trans: " + data.OuterXml.ToString());
                        return false;
                    }
                    break;
                case TransitionType.Exit:
                    if (!machine.TryAddExitTrans(from, events))
                    {
                        LogMgr.Instance.Error("Invalid exit trans: " + data.OuterXml.ToString());
                        return false;
                    }
                    break;
                default:
                    if (!machine.TryAddTrans(from, to, events))
                    {
                        LogMgr.Instance.Error("Invalid trans: " + data.OuterXml.ToString());
                        return false;
                    }
                    break;
            }

            return true;
        }
        protected bool _LoadEntryTrans(FSMMachineNode machine, XmlNode data)
        {
            var attr = data.Attributes["To"];
            string to = string.Empty;
            if (attr != null)
                to = attr.Value;

            List<string> events = new List<string>();
            foreach (XmlNode chi in data.ChildNodes)
            {
                events.Add(chi.Name);
            }

            if (!machine.TryAddEntryTrans(to, events))
            {
                LogMgr.Instance.Error("Invalid trans: " + data.OuterXml.ToString());
                return false;
            }

            return true;
        }
        protected bool _LoadExitTrans(FSMMachineNode machine, XmlNode data)
        {
            var attr = data.Attributes["From"];
            string from = string.Empty;
            if (attr != null)
                from = attr.Value;

            List<string> events = new List<string>();
            foreach (XmlNode chi in data.ChildNodes)
            {
                events.Add(chi.Name);
            }

            if (!machine.TryAddExitTrans(from, events))
            {
                LogMgr.Instance.Error("Invalid trans: " + data.OuterXml.ToString());
                return false;
            }

            return true;
        }

        void _RefreshStackMachines(FSMNode node)
        {
            if (node != null && node is FSMMachineNode)
            {
                FSMMachineNode old = CurMachine;
                using (var v = StackMachines.Delay())
                {
                    StackMachines.Clear();
                    FSMMachineNode cur = node as FSMMachineNode;
                    while (cur != null)
                    {
                        StackMachines.Add(cur);
                        cur = cur.MetaState?.OwnerMachine;
                    }
                }
            }
        }

        public override void SaveSuo()
        {
            string fileName = this.FileInfo.RelativeName;
            Config.Instance.Suo.ResetFile(fileName);
            Action<FSMStateNode> func = (FSMStateNode node) =>
            {
                if (!node.DebugPointInfo.NoDebugPoint)
                {
                    Config.Instance.Suo.SetDebugPointInfo(fileName, node.UID, node.DebugPointInfo.HitCount);
                }
            };
            Utility.ForEachFSMState(m_FSM, func);
        }
        public override void Save(XmlElement data, XmlDocument xmlDoc)
        {
            SaveSuo();
            CommandMgr.Blocked = true;
            _SaveMachine(m_FSM.RootMachine, data, xmlDoc, false);
            CommandMgr.Blocked = false;

            CommandMgr.Dirty = false;
            m_ExportFileHash = 0;

            OnPropertyChanged("ShortDisplayName");
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

            {
                foreach (Transition trans in machine.LocalTransition)
                {
                    _SaveTrans(trans, nodeEl, xmlDoc);
                }
            }

            if (machine is FSMRootMachineNode)
            {
                foreach (Transition trans in (machine as FSMRootMachineNode).Transition)
                {
                    _SaveTrans(trans, nodeEl, xmlDoc);
                }
            }
        }

        void _SaveState(FSMStateNode state, XmlElement data, XmlDocument xmlDoc, bool bExport)
        {
            if (bExport && (state is FSMAnyStateNode || state is FSMUpperStateNode))
                return;
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

        void _SaveTrans(Transition trans, XmlElement data, XmlDocument xmlDoc)
        {
            XmlElement nodeEl = xmlDoc.CreateElement("Trans");
            data.AppendChild(nodeEl);

            if (trans.Type == TransitionType.Entry)
                nodeEl.SetAttribute("Type", "Entry");
            else if (trans.Type == TransitionType.Exit)
                nodeEl.SetAttribute("Type", "Exit");

            if (trans.Key.FromState != null && trans.Type != TransitionType.Entry)
                nodeEl.SetAttribute("From", trans.Key.FromState.NickName);

            if (trans.Key.ToState != null && trans.Type != TransitionType.Exit)
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

                foreach (Connector ctr in state.Conns.MainConnectors)
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
            foreach (Connector ctr in node.Conns.MainConnectors)
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

            int idx = 0;
            while(idx < conn.Trans.Count)
            {
                ///> Some trans cant be deleted from here. Keep it and inc the idx to delete next one.
                if (!_Disconnect(conn, conn.Trans[idx]))
                    ++idx;
            }
        }
        /// <summary>
        /// Disconnect the transition of two states,
        /// and if there's no more transition between these two states, disconnect them and remove the viewmodel of line.
        /// </summary>
        /// <param name="fromto"></param>
        /// <param name="trans"></param>
        public void Disconnect(Connection.FromTo fromto, Transition trans)
        {
            FSMConnection conn = fromto.From.FindConnection(fromto) as FSMConnection;
            if (conn == null)
                return;
            _Disconnect(conn, trans);
        }

        bool _Disconnect(FSMConnection conn, Transition trans)
        {
            ConnectionRenderer connectionRenderer = conn.Renderer;
            if (CurMachine.RemoveTrans(trans))
            {
                if (conn.Trans.Count == 0)
                {
                    if (connectionRenderer != null)
                        ConnectionList.Remove(connectionRenderer);

                }

                PushDoneCommand(new RemoveTransCommand()
                {
                    Conn = conn.Ctr,
                    Trans = trans,
                });

                return true;
            }
            return false;
        }

        public override void ConnectNodes(Connector from, Connector to)
        {
            _MakeTrans(from, to);
        }

        Transition _MakeTrans(Connector from, Connector to)
        {
            if (from == null || to == null || from.GetDir != Connector.Dir.OUT || to.GetDir != Connector.Dir.IN)
                return null;
            ///>From same owner. For now we dont support this.
            if (from.Owner == to.Owner)
                return null;

            var fromNode = from.Owner as FSMStateNode;
            var toNode = to.Owner as FSMStateNode;
            if (fromNode.Type == FSMStateType.Special && toNode is FSMExitStateNode)
                return null;
            var res = CurMachine.MakeTrans(fromNode, toNode);
            _OnTransMade(from, to, ref res);
            return res.Trans;
        }
        /// <summary>
        /// Try to make a new transition between two states
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="existTrans">if null, a new transition will be created. Or a transition will be added to it</param>
        /// <returns></returns>
        public Transition MakeTrans(Connector from, Connector to, Transition existTrans)
        {
            if (from == null || to == null || from.GetDir != Connector.Dir.OUT || to.GetDir != Connector.Dir.IN)
                return null;
            ///>From same owner. For now we dont support this.
            if (from.Owner == to.Owner)
                return null;
            var fromNode = from.Owner as FSMStateNode;
            var toNode = to.Owner as FSMStateNode;
            if (fromNode.Type == FSMStateType.Special && toNode is FSMExitStateNode)
                return null;
            var res = CurMachine.MakeTrans(existTrans);
            _OnTransMade(from, to, ref res);
            return res.Trans;
        }


        void _OnTransMade(Connector from, Connector to, ref TransitionResult res)
        {
            if (res.Route.Route != null)
            {
                foreach (var p in res.Route.Route)
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

            PushDoneCommand(new MakeTransCommand()
            {
                Conn = new Connection.FromTo { From = from, To = to },
                Trans = res.Trans,
            });
        }
        /// <summary>
        /// Clear the default state
        /// </summary>
        /// <param name="machine"></param>
        public void ResetDefault(FSMMachineNode machine)
        {
            _SetDefault(machine, null);
        }
        /// <summary>
        /// Set the state default 
        /// </summary>
        /// <param name="state"></param>
        public void SetDefault(FSMStateNode state)
        {
            FSMMachineNode machine = state == null ? CurMachine : state.OwnerMachine;
            _SetDefault(machine, state);
        }

        void _SetDefault(FSMMachineNode machine, FSMStateNode state)
        {
            FSMConnection oldConn = null;
            FSMConnection newConn = null;

            if (machine.SetDefault(state, ref oldConn, ref newConn))
            {
                if (oldConn != null && oldConn.Trans.Count == 0)
                    ConnectionList.Remove(oldConn.Renderer);
                if (newConn != null && newConn.Trans.Count == 1)
                    ConnectionList.Add(newConn.Renderer);

                PushDoneCommand(new SetDefaultStateCommand()
                {
                    Origin = oldConn?.Ctr.To.Owner as FSMStateNode,
                    Final = state,
                });
            }
        }

        public override void AddNode(NodeBase node)
        {
            FSMStateNode state = node as FSMStateNode;

            Utility.OperateNode(node, m_Graph, NodeBase.OnAddToGraph);

            if (!CurMachine.AddState(state))
                return;

            _AddStateRenderer(state);
            SelectionMgr.Instance.Clear();

            NodeList.Dispose();

            m_FSM.RefreshNodeUID(0);


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
