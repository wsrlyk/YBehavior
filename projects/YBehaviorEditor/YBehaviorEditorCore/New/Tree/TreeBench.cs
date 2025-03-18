using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace YBehavior.Editor.Core.New
{
    /// <summary>
    /// Operating a tree
    /// </summary>
    public class TreeBench : WorkBench
    {
        /// <summary>
        /// This will not be decreased when removing a tree in the forest.
        /// Just for getting a proper start uid for the forest.
        /// </summary>
        uint m_TotalForestCount = 0;
        /// <summary>
        /// Main tree is excluded
        /// </summary>
        List<TreeNode> m_Forest = new List<TreeNode>();

        /// <summary>
        /// Include the MainTree and Forest
        /// </summary>
        List<TreeNode> m_AllTrees = new List<TreeNode>();

        Tree m_Tree;
        /// <summary>
        /// The main tree
        /// </summary>
        public Tree Tree { get { return m_Tree; } }
        public TreeBench()
        {
            m_Tree = new Tree();
            m_Graph = m_Tree;
            m_AllTrees.Add(m_Tree.Root);
        }

        public override void InitEmpty()
        {
            NodeBase.OnAddToGraph(m_Tree.Root, m_Graph);
            _SetGUID(m_Tree.Root);
            //Utility.OperateNode(m_Tree.Root, m_Graph, false, NodeBase.OnAddToGraph);
            m_Tree.RefreshNodeUID(0);
            AddRenderers(m_Tree.Root, true);
        }
        /// <summary>
        /// Try to connect an OUTPUT pin and an INPUT pin
        /// </summary>
        /// <param name="ctr0"></param>
        /// <param name="ctr1"></param>
        public void ConnectVariables(Connector ctr0, Connector ctr1)
        {
            Connector parent;
            Connector child;
            Connection conn = null;
            conn = Connector.TryConnect(ctr0, ctr1, out parent, out child);
            if (conn != null)
            {
                ///> refresh parent connections
                ConnectionRenderer connRenderer = conn.Renderer;
                if (connRenderer != null)
                    ConnectionList.Add(connRenderer);
            }
        }

        public override void ConnectNodes(Connector ctr0, Connector ctr1)
        {
            Connector parent;
            Connector child;
            Connection conn = null;
            conn = Connector.TryConnect(ctr0, ctr1, out parent, out child);

            if (conn != null)
            {
                ///> refresh parent connections
                ConnectionRenderer connRenderer = conn.Renderer;
                if (connRenderer != null)
                    ConnectionList.Add(connRenderer);

                if (child.GetPosType == Connector.PosType.PARENT)
                {
                    TreeNode childNode = child.Owner as TreeNode;
                    RemoveForestTree(childNode, false);

                    m_Tree.RefreshNodeUID(0);
                }

                ConnectNodeCommand connectNodeCommand = new ConnectNodeCommand
                {
                    Conn = conn.Ctr,
                };
                PushDoneCommand(connectNodeCommand);
            }
            else
            {
                ///> errorcode
            }
        }


        public override void DisconnectNodes(Connection.FromTo connection)
        {
            ConnectionRenderer connectionRenderer = connection.From.GetRenderer(connection);
            if (Connector.TryDisconnect(connection))
            {
                if (connectionRenderer != null)
                    ConnectionList.Remove(connectionRenderer);

                if (connection.To.GetPosType == Connector.PosType.PARENT)
                {
                    TreeNode childNode = connection.To.Owner as TreeNode;
                    AddForestTree(childNode, false);

                    m_Tree.RefreshNodeUID(0);
                    _RefreshForestUID(childNode);
                }

                DisconnectNodeCommand disconnectNodeCommand = new DisconnectNodeCommand
                {
                    Conn = connection,
                };
                PushDoneCommand(disconnectNodeCommand);
            }
            else
            {
                ///> errorcode
            }
        }


        public override void RemoveNode(NodeBase node)
        {
            RemoveForestTree(node as TreeNode);

            RemoveNodeCommand removeNodeCommand = new RemoveNodeCommand()
            {
                Node = node
            };
            PushDoneCommand(removeNodeCommand);
        }

        public override void AddNode(NodeBase node)
        {
            Utility.OperateNode(node, m_Graph, (NodeBase n, Graph g) => 
            {
                NodeBase.OnAddToGraph(n, g);
                _SetGUID(n);
            });
            _RefreshForestUID(node);
            AddForestTree(node as TreeNode, true);

            AddNodeCommand addNodeCommand = new AddNodeCommand()
            {
                Node = node
            };
            PushDoneCommand(addNodeCommand);
        }

        public override void OnNodeMoved(EventArg arg)
        {
            NodeMovedArg oArg = arg as NodeMovedArg;
            TreeNode parent = (oArg.Node as TreeNode).Parent;
            if (parent != null)
                m_Tree.RefreshNodeUIDFromMiddle(parent);
        }

        public override bool Load(XmlElement data, bool bRendering)
        {
            CommandMgr.Blocked = true;

            foreach (XmlNode chi in data.ChildNodes)
            {
                if (chi.Name == "Node")
                {
                    var attr = chi.Attributes["Class"];
                    if (attr == null)
                        continue;
                    if (attr.Value == "Root")
                    {
                        _LoadTree(m_Tree.Root, chi);
                        m_Tree.RefreshNodeUID(0);
                        _LoadSuo(m_Tree.Root);
                        AddRenderers(m_Tree.Root, false);
                    }
                    else
                    {
                        TreeNode node = TreeNodeMgr.Instance.CreateNodeByName(attr.Value);
                        if (node == null)
                        {
                            EventMgr.Instance.Send(new ShowSystemTipsArg
                            {
                                Content = "Unknown node: " + attr.Value,
                                TipType = ShowSystemTipsArg.TipsType.TT_Error,
                            });
                            return false;
                        }
                        _LoadTree(node, chi);
                        _RefreshForestUID(node);
                        _LoadSuo(node);
                        AddForestTree(node, true);
                    }
                }
                else if (chi.Name == "DataConnections")
                {
                    _LoadVariableConnections(chi);
                }
                else if (chi.Name == "Comments")
                {
                    _LoadComments(chi);
                }
            }

            _RefreshReferenceStates();
            _RefreshGUID();
            CommandMgr.Blocked = false;
            return true;
        }

        void _LoadSuo(TreeNode tree)
        {
            string fileName = this.FileInfo.RelativeName;
            var map = Config.Instance.Suo.GetSuo(fileName);
            if (map != null)
            {
                Action<NodeBase> action = (NodeBase node) =>
                {
                    node.DebugPointInfo.HitCount = map.GetDebugPoint(node.UID);
                    if (map.GetFold(node.UID))
                        (node as TreeNode).Folded = true;
                };
                Utility.OperateNode(tree, action);
            }
        }

        private bool _LoadTree(TreeNode tree, XmlNode data)
        {
            m_Tree.SetFlag(Graph.FLAG_LOADING);
            tree.Load(data);
            bool bRes = _LoadOneNode(tree, data);
            m_Tree.RemoveFlag(Graph.FLAG_LOADING);
            ////Utility.InitNode(tree, true);
            return bRes;
        }

        private bool _LoadOneNode(TreeNode node, XmlNode data)
        {
            if (node == null)
                return false;

            ///> We have to load the data BEFORE connecting it to its parent, 
            ///  to make sure all its own properties are correctly set
            //node.Load(data);
            NodeBase.OnAddToGraph(node, m_Graph);
            //Utility.OperateNode(node, m_Graph, false, NodeBase.OnAddToGraph);

            foreach (XmlNode chi in data.ChildNodes)
            {
                if (chi.Name == "Node")
                {
                    var attr = chi.Attributes["Class"];
                    if (attr == null)
                        continue;
                    TreeNode childNode = TreeNodeMgr.Instance.CreateNodeByName(attr.Value);
                    if (childNode == null)
                    {
                        EventMgr.Instance.Send(new ShowSystemTipsArg
                        {
                            Content = "Unknown node: " + attr.Value,
                            TipType = ShowSystemTipsArg.TipsType.TT_Error,
                        });
                        return false;
                    }

                    string connectionIdentifier = Connector.IdentifierChildren;
                    attr = chi.Attributes["Connection"];
                    if (attr != null)
                        connectionIdentifier = attr.Value;

                    childNode.Load(chi);
                    node.Conns.Connect(childNode, connectionIdentifier);
                    _LoadOneNode(childNode, chi);
                }
                else
                {
                    node.LoadChild(chi);
                }
            }
            node.Conns.Sort(Connections.SortByPosX);
            if (node.HasConditionConnection)
                node.EnableCondition = true;
            return true;
        }
        private bool _LoadVariableConnections(XmlNode data)
        {
            foreach (XmlNode chi in data.ChildNodes)
            {
                uint FromGUID;
                uint ToGUID;
                string FromName;
                string ToName;
                var attr = chi.Attributes["FromGUID"];
                if (attr == null)
                {
                    LogMgr.Instance.Error("No FromGUID Attribute");
                    continue;
                }
                if (!uint.TryParse(attr.Value, out FromGUID))
                {
                    LogMgr.Instance.Error("Parse FromUID Attribute Error: " + attr.Value);
                    continue;
                }
                attr = chi.Attributes["ToGUID"];
                if (attr == null)
                {
                    LogMgr.Instance.Error("No ToGUID Attribute");
                    continue;
                }
                if (!uint.TryParse(attr.Value, out ToGUID))
                {
                    LogMgr.Instance.Error("Parse ToGUID Attribute Error: " + attr.Value);
                    continue;
                }
                attr = chi.Attributes["FromName"];
                if (attr == null)
                {
                    LogMgr.Instance.Error("No FromName Attribute");
                    continue;
                }
                FromName = attr.Value;
                attr = chi.Attributes["ToName"];
                if (attr == null)
                {
                    LogMgr.Instance.Error("No ToName Attribute");
                    continue;
                }
                ToName = attr.Value;

                NodeBase target = null;
                TreeNode fromNode = null;
                TreeNode toNode = null;
                Func<NodeBase, uint, bool> func = (NodeBase node, uint guid) =>
                {
                    if (node.GUID == guid)
                    {
                        target = node;
                        return true;
                    }
                    return false;
                };

                foreach (TreeNode tree in m_AllTrees)
                {
                    if (Utility.OperateNode(tree, FromGUID, func))
                    {
                        fromNode = target as TreeNode;
                        break;
                    }
                }
                if (fromNode == null)
                {
                    LogMgr.Instance.Error("Cant find FromGUID " + FromGUID);
                    continue;
                }
                foreach (TreeNode tree in m_AllTrees)
                {
                    if (Utility.OperateNode(tree, ToGUID, func))
                    {
                        toNode = target as TreeNode;
                        break;
                    }
                }
                if (toNode == null)
                {
                    LogMgr.Instance.Error("Cant find ToGUID " + ToGUID);
                    continue;
                }

                var fromCtr = fromNode.Conns.GetConnector(FromName, Connector.PosType.OUTPUT);
                if (fromCtr == null)
                {
                    LogMgr.Instance.Error("Cant find output Variable in FromNode, name = " + FromName);
                    continue;
                }
                var toCtr = toNode.Conns.GetConnector(ToName, Connector.PosType.INPUT);
                if (toCtr == null)
                {
                    LogMgr.Instance.Error("Cant find input Variable in ToNode, name = " + ToName);
                    continue;
                }
                ConnectVariables(fromCtr, toCtr);
            }
            return true;
        }
        public override void SaveSuo()
        {
            string fileName = this.FileInfo.RelativeName;
            Config.Instance.Suo.ResetFile(fileName);
            Action<NodeBase> func = (NodeBase node) =>
            {
                if (!node.DebugPointInfo.NoDebugPoint)
                {
                    Config.Instance.Suo.SetDebugPointInfo(fileName, node.UID, node.DebugPointInfo.HitCount);
                }
                if ((node as TreeNode).Folded)
                    Config.Instance.Suo.SetFoldInfo(fileName, node.UID, true);
            };

            Utility.OperateNode(m_Tree.Root, func);
            m_TotalForestCount = 0;
            foreach (var tree in m_Forest)
            {
                _RefreshForestUID(tree);
                Utility.OperateNode(tree, func);
            }
        }

        public override void Save(XmlElement data, XmlDocument xmlDoc) 
        {
            m_Tree.RefreshNodeUID(0);

            _RefreshReferenceStates();

            SaveSuo();
            //_SaveNode(m_Tree.Root, data, xmlDoc);

            foreach (var tree in m_AllTrees)
            {
                _SaveNode(tree, data, xmlDoc);
            }


            {
                XmlElement variableRoot = null;
                //_SaveVariableConnections(m_Tree.Root, ref variableRoot, data, xmlDoc);
                foreach (var tree in m_AllTrees)
                {
                    _SaveVariableConnections(tree, ref variableRoot, data, xmlDoc, false);
                }
            }

            _SaveComments(data, xmlDoc);

            CommandMgr.Dirty = false;
            m_ExportFileHash = 0;

            OnPropertyChanged("ShortDisplayName");
        }

        void _SaveNode(TreeNode node, XmlElement data, XmlDocument xmlDoc)
        {
            XmlElement nodeEl = xmlDoc.CreateElement("Node");
            data.AppendChild(nodeEl);

            nodeEl.SetAttribute("Class", node.Name);
            node.Save(nodeEl, xmlDoc);

            foreach (TreeNode chi in node.Conns)
            {
                _SaveNode(chi, nodeEl, xmlDoc);
            }
        }

        void _SaveVariableConnections(TreeNode node, ref XmlElement data, XmlElement root, XmlDocument xmlDoc, bool export)
        {
            ///> We get the connection NOT from the InputConnector but the OutputConnector,
            ///  to make sure that the connections are sorted by From's UID
            foreach (Connector c in node.Conns.OutputConnectors)
            {
                foreach (Connection conn in c.Conns)
                {
                    if (export && conn.Ctr.From.Owner.Disabled && conn.Ctr.To.Owner.Disabled)
                        continue;
                    if (data == null)
                    {
                        data = xmlDoc.CreateElement("DataConnections");
                        root.AppendChild(data);
                    }
                    var el = xmlDoc.CreateElement("DataConnection");
                    data.AppendChild(el);
                    if (export)
                        el.SetAttribute("FromUID", conn.Ctr.From.Owner.UID.ToString());
                    else
                        el.SetAttribute("FromGUID", conn.Ctr.From.Owner.GUID.ToString());
                    el.SetAttribute("FromName", conn.Ctr.From.Identifier);
                    if (export)
                        el.SetAttribute("ToUID", conn.Ctr.To.Owner.UID.ToString());
                    else
                        el.SetAttribute("ToGUID", conn.Ctr.To.Owner.GUID.ToString());
                    el.SetAttribute("ToName", conn.Ctr.To.Identifier);
                }
            }

            foreach (TreeNode chi in node.Conns)
            {
                _SaveVariableConnections(chi, ref data, root, xmlDoc, export);
            }
        }
        public override void Export(XmlElement data, XmlDocument xmlDoc)
        {
            _ExportNode(m_Tree.Root, data, xmlDoc);
            XmlElement variableRoot = null;
            _SaveVariableConnections(m_Tree.Root, ref variableRoot, data, xmlDoc, true);

            m_ExportFileHash = _GenerateHash(data.OuterXml.Replace(" ", string.Empty));
        }

        void _ExportNode(TreeNode node, XmlElement data, XmlDocument xmlDoc)
        {
            if (node.Disabled)
                return;

            XmlElement nodeEl = xmlDoc.CreateElement("Node");
            data.AppendChild(nodeEl);

            nodeEl.SetAttribute("Class", node.Name);
            node.Export(nodeEl, xmlDoc);

            foreach (TreeNode chi in node.Conns)
            {
                _ExportNode(chi, nodeEl, xmlDoc);
            }
        }

        /// <summary>
        /// Remove a tree from forest
        /// </summary>
        /// <param name="root"></param>
        /// <param name="bRemoveRenderer">When true, its viewmodel will be removed in this function</param>
        public void RemoveForestTree(TreeNode root, bool bRemoveRenderer = true)
        {
            if (root == null)
                return;

            if (bRemoveRenderer)
                RemoveRenderers(root);

            m_AllTrees.Remove(root);
            m_Forest.Remove(root);
        }
        /// <summary>
        /// Add a tree to forest
        /// </summary>
        /// <param name="root"></param>
        /// <param name="bAddRenderer">When true, its viewmodel will be added in this function</param>
        public void AddForestTree(TreeNode root, bool bAddRenderer)
        {
            if (root == null)
                return;
            m_Forest.Add(root);
            m_AllTrees.Add(root);

            if (bAddRenderer)
                AddRenderers(root, true);
        }

        void _RefreshForestUID(NodeBase root)
        {
            m_Tree.RefreshNodeUIDFromRoot(root, ++m_TotalForestCount * 1000);
        }

        public override void AddRenderers(NodeBase node, bool batchAdd, bool excludeRoot = false)
        {
            _AddRenderers(node as TreeNode, excludeRoot);

            SelectionMgr.Instance.Clear();

            NodeList.Dispose();
            ConnectionList.Dispose();
        }

        void _AddRenderers(TreeNode node, bool excludeRoot)
        {
            if (!excludeRoot)
                NodeList.DelayAdd(node.ForceGetRenderer);

            if (!node.IsChildrenRendering)
                return;

            foreach (NodeBase chi in node.Conns)
            {
                _AddRenderers(chi as TreeNode, false);
            }

            foreach (Connector ctr in node.Conns.MainConnectors)
            {
                foreach(Connection conn in ctr.Conns)
                {
                    ConnectionList.DelayAdd(conn.Renderer);
                }
            }
        }

        public override void RemoveRenderers(NodeBase node, bool excludeRoot = false)
        {
            //using (var v1 = ConnectionList.Delay())
            {
                //using (var v2 = NodeList.Delay())
                {
                    _RemoveRenderers(node, excludeRoot);
                }
            }
        }

        void _RemoveRenderers(NodeBase node, bool excludeRoot)
        {
            foreach (Connector ctr in node.Conns.MainConnectors)
            {
                foreach (Connection conn in ctr.Conns)
                {
                    ConnectionList.Remove(conn.Renderer);
                }
            }

            bool bNeedRemoveChildren = true;
            if (!excludeRoot)
                bNeedRemoveChildren = NodeList.Remove(node.Renderer);

            if (bNeedRemoveChildren)
            {
                foreach (NodeBase chi in node.Conns)
                {
                    _RemoveRenderers(chi, false);
                }
            }
        }

        public override bool CheckError()
        {
            bool bRes = _CheckError(m_Tree.Root);
            bRes &= _CheckVariableConnections(m_Tree.Root);
            return bRes;
        }

        private bool _CheckError(TreeNode root)
        {
            bool bRes = true;

            Utility.OperateNode(root, (NodeBase node) =>
            {
                if (!node.CheckValid())
                    bRes = false;
                foreach (VariableHolder v in (node as TreeNode).Variables.Datas)
                {
                    if (!v.Variable.CheckValid())
                    {
                        LogMgr.Instance.Error("CheckError in Node: " + node.Renderer.UITitle + ", Variable: " + v.Variable.Name);
                        bRes = false;
                    }
                }
            }
            );
            return bRes;
        }

        bool _CheckVariableConnections(TreeNode root)
        {
            bool bRes = true;
            Utility.OperateNode(root, (NodeBase node) =>
            {
                TreeNode thisNode = node as TreeNode;
                if (thisNode.Disabled)
                    return;

                TreeNode rootNode = thisNode.Root;
                foreach (Connector ctr in node.Conns.OutputConnectors)
                {
                    if (!ctr.IsVisible)
                    {
                        if (ctr.Conns.Count > 0)
                        {
                            LogMgr.Instance.Error("CheckError in Node: " + node.Renderer.UITitle + ", Connector: " + ctr.Identifier);
                            bRes = false;
                        }
                        continue;
                    }
                    Variable fromVariable = thisNode.Variables.GetVariable(ctr.Identifier);
                    if (fromVariable == null)
                    {
                        LogMgr.Instance.Error("CheckError in Node: " + node.Renderer.UITitle + ", Connector: " + ctr.Identifier);
                        bRes = false;
                        continue;
                    }
                    if (!fromVariable.ShouldHaveConnection)
                    {
                        LogMgr.Instance.Error("CheckError in Node: " + node.Renderer.UITitle + ", Connector: " + ctr.Identifier);
                        bRes = false;
                        continue;
                    }
                    if (ctr.Conns.Count == 0)
                    {
                        LogMgr.Instance.Error("CheckError in Node: " + node.Renderer.UITitle + ", Connector: " + ctr.Identifier);
                        bRes = false;
                        continue;
                    }

                    foreach (Connection conn in ctr.Conns)
                    {
                        Connector toCtr = conn.Ctr.To;
                        TreeNode toNode = toCtr.Owner as TreeNode;
                        if (toNode.Root != rootNode)
                        {
                            LogMgr.Instance.Error("CheckError in Node: " + toNode.Renderer.UITitle + ", Not in Same Tree with: " + node.Renderer.UITitle);
                            bRes = false;
                            continue;
                        }
                        if (toNode.Disabled)
                        {
                            LogMgr.Instance.Error("CheckError in Node: " + toNode.Renderer.UITitle + ", Disabled, Can NOT connect to: " + node.Renderer.UITitle);
                            bRes = false;
                            continue;
                        }
                        Variable toVariable = toNode.Variables.GetVariable(toCtr.Identifier);
                        if (toVariable == null)
                        {
                            LogMgr.Instance.Error("CheckError in Node: " + toNode.Renderer.UITitle + ", Connector: " + toCtr.Identifier);
                            bRes = false;
                            continue;
                        }
                        if (!toVariable.ShouldHaveConnection)
                        {
                            LogMgr.Instance.Error("CheckError in Node: " + toNode.Renderer.UITitle + ", Connector: " + toCtr.Identifier);
                            bRes = false;
                            continue;
                        }
                    }
                }
                foreach (Connector ctr in node.Conns.InputConnectors)
                {
                    if (!ctr.IsVisible)
                    {
                        if (ctr.Conns.Count > 0)
                        {
                            LogMgr.Instance.Error("CheckError in Node: " + node.Renderer.UITitle + ", Connector: " + ctr.Identifier);
                            bRes = false;
                        }
                        continue;
                    }
                    Variable toVariable = thisNode.Variables.GetVariable(ctr.Identifier);
                    if (toVariable == null)
                    {
                        LogMgr.Instance.Error("CheckError in Node: " + node.Renderer.UITitle + ", Connector: " + ctr.Identifier);
                        bRes = false;
                        continue;
                    }
                    if (!toVariable.ShouldHaveConnection)
                    {
                        LogMgr.Instance.Error("CheckError in Node: " + node.Renderer.UITitle + ", Connector: " + ctr.Identifier);
                        bRes = false;
                        continue;
                    }
                    if (ctr.Conns.Count == 0)
                    {
                        LogMgr.Instance.Error("CheckError in Node: " + node.Renderer.UITitle + ", Connector: " + ctr.Identifier);
                        bRes = false;
                        continue;
                    }

                    foreach (Connection conn in ctr.Conns)
                    {
                        Connector fromCtr = conn.Ctr.From;
                        TreeNode fromNode = fromCtr.Owner as TreeNode;
                        if (fromNode.Root != rootNode)
                        {
                            LogMgr.Instance.Error("CheckError in Node: " + fromNode.Renderer.UITitle + ", Not in Same Tree with: " + node.Renderer.UITitle);
                            bRes = false;
                            continue;
                        }
                        if (fromNode.Disabled)
                        {
                            LogMgr.Instance.Error("CheckError in Node: " + fromNode.Renderer.UITitle + ", Disabled, Can NOT connect to: " + node.Renderer.UITitle);
                            bRes = false;
                            continue;
                        }
                    }
                }
            }
            );
           return bRes;
        }
        /// <summary>
        /// Each node will have a GUID. The newly added node will NOT have the GUID of the removed node
        /// </summary>
        uint m_GUID;
        void _RefreshGUID()
        {
            m_GUID = 0;
            foreach (var tree in m_AllTrees)
            {
                Utility.OperateNode(tree, (NodeBase node) =>
                { 
                    if (node.GUID != 0)
                        m_GUID = Math.Max(m_GUID, node.GUID);
                });
            }
            foreach (var tree in m_AllTrees)
            {
                Utility.OperateNode(tree, (NodeBase node) =>
                {
                    if (node.GUID == 0)
                        node.GUID = ++m_GUID;
                });
            }
        }

        void _SetGUID(NodeBase node)
        {
            if (node.GUID == 0)
                node.GUID = ++m_GUID;
        }
        /// <summary>
        /// Check if the variables are referenced by the nodes
        /// </summary>
        void _RefreshReferenceStates()
        {
            foreach (var v in m_Tree.TreeMemory.SharedMemory.Datas)
            {
                v.Variable.referencedType = Variable.ReferencedType.None;
            }
            foreach (var v in m_Tree.TreeMemory.LocalMemory.Datas)
            {
                v.Variable.referencedType = Variable.ReferencedType.None;
            }

            Action<Variable, TreeMemory, bool, bool> processor = (Variable v, TreeMemory memory, bool bIsMainTree, bool bIsDisabled) =>
           {
               if (v.vbType == Variable.VariableType.VBT_Const)
                   return;

               if (!v.CheckValid())
                   return;

               Variable r = memory.GetVariable(v.Value, v.IsLocal);
               if (r != null)
                   r.TrySetReferencedType(bIsMainTree && !bIsDisabled && v.eType != Variable.EnableType.ET_Disable ? Variable.ReferencedType.Active : Variable.ReferencedType.Disactive);
           };

            Action<NodeBase, Tree, bool> handler =
            (NodeBase node, Tree tree, bool isMainTree) =>
            {
                if (node is TreeNode)
                {
                    if (node is RootTreeNode)
                        return;

                    if (tree == null)
                        return;

                    TreeMemory treeMemory = tree.TreeMemory;

                    TreeNode treeNode = node as TreeNode;
                    foreach (var v in treeNode.NodeMemory.Datas)
                    {
                        processor(v.Variable, treeMemory, isMainTree, node.Disabled);
                        if (v.Variable.IsElement)
                        {
                            processor(v.Variable.VectorIndex, treeMemory, isMainTree, node.Disabled);
                        }
                    }

                    if (treeNode is SubTreeNode)
                    {
                        foreach (var v in (treeNode as SubTreeNode).InOutMemory.InputMemory.Datas)
                        {
                            processor(v.Variable, treeMemory, isMainTree, node.Disabled);
                            if (v.Variable.IsElement)
                            {
                                processor(v.Variable.VectorIndex, treeMemory, isMainTree, node.Disabled);
                            }
                        }
                        foreach (var v in (treeNode as SubTreeNode).InOutMemory.OutputMemory.Datas)
                        {
                            processor(v.Variable, treeMemory, isMainTree, node.Disabled);
                            if (v.Variable.IsElement)
                            {
                                processor(v.Variable.VectorIndex, treeMemory, isMainTree, node.Disabled);
                            }
                        }
                    }
                }
            };

            Utility.OperateNode(m_Tree.Root, m_Tree, true, handler);
            foreach (var tree in m_Forest)
            {
                Utility.OperateNode(tree, m_Tree, false, handler);
            }

            foreach (var v in m_Tree.InOutMemory.InputMemory.Datas)
            {
                processor(v.Variable, m_Tree.TreeMemory, true, false);
                if (v.Variable.IsElement)
                {
                    processor(v.Variable.VectorIndex, m_Tree.TreeMemory, true, false);
                }
            }
            foreach (var v in m_Tree.InOutMemory.OutputMemory.Datas)
            {
                processor(v.Variable, m_Tree.TreeMemory, true, false);
                if (v.Variable.IsElement)
                {
                    processor(v.Variable.VectorIndex, m_Tree.TreeMemory, true, false);
                }
            }

        }
        /// <summary>
        /// Switch a variable between shared and local collection
        /// </summary>
        /// <param name="v"></param>
        public void Switch(Variable v)
        {
            if (m_Tree.SharedData.SwitchVariable(v))
            {
                _RefreshAfterSwitchVariable(v);
                LogMgr.Instance.Log("Switch " + v.DisplayName);
            }
        }

        bool _RefreshAfterSwitchVariable(Variable variable)
        {
            if (variable == null)
                return false;

            Action<Variable> processor =
                (Variable target) =>
                {
                    if (target.Value == variable.Name && target.IsLocal == !variable.IsLocal)
                    {
                        target.IsLocal = variable.IsLocal;
                    }
                };

            Action<NodeBase> handler =
            (NodeBase node) =>
            {
                if (node is TreeNode)
                {
                    if (node is RootTreeNode)
                        return;

                    TreeMemory treeMemory = m_Tree.TreeMemory;

                    TreeNode treeNode = node as TreeNode;
                    foreach (var v in treeNode.NodeMemory.Datas)
                    {
                        processor(v.Variable);
                        if (v.Variable.IsElement)
                        {
                            processor(v.Variable.VectorIndex);
                        }
                    }

                    if (treeNode is SubTreeNode)
                    {
                        foreach (var v in (treeNode as SubTreeNode).InOutMemory.InputMemory.Datas)
                        {
                            processor(v.Variable);
                            if (v.Variable.IsElement)
                            {
                                processor(v.Variable.VectorIndex);
                            }
                        }
                        foreach (var v in (treeNode as SubTreeNode).InOutMemory.OutputMemory.Datas)
                        {
                            processor(v.Variable);
                            if (v.Variable.IsElement)
                            {
                                processor(v.Variable.VectorIndex);
                            }
                        }
                    }
                }
            };

            //Utility.OperateNode(m_Tree.Root, handler);
            foreach (var tree in m_AllTrees)
            {
                Utility.OperateNode(tree, handler);
            }

            foreach (var v in m_Tree.InOutMemory.InputMemory.Datas)
            {
                processor(v.Variable);
                if (v.Variable.IsElement)
                {
                    processor(v.Variable.VectorIndex);
                }
            }
            foreach (var v in m_Tree.InOutMemory.OutputMemory.Datas)
            {
                processor(v.Variable);
                if (v.Variable.IsElement)
                {
                    processor(v.Variable.VectorIndex);
                }
            }

            return true;
        }
        /// <summary>
        /// Check if any shared variables could be local.
        /// This function is for refinement.
        /// </summary>
        public void CheckLocal()
        {
            Dictionary<VariableHolder, List<Tuple<bool, int, int>>> res = new Dictionary<VariableHolder, List<Tuple<bool, int, int>>>();
            Action<Variable, int, int, bool> processor = (Variable v, int uid, int parentuid, bool isInput) =>
            {
                if (v.vbType == Variable.VariableType.VBT_Const)
                    return;

                if (v.IsLocal)
                    return;

                if (!v.CheckValid())
                    return;

                VariableHolder vh = m_Tree.TreeMemory.GetVariableHolder(v.Value, v.IsLocal);
                if (vh != null)
                {
                    if (!res.TryGetValue(vh, out var list))
                    {
                        list = new List<Tuple<bool, int, int>>();
                        res.Add(vh, list);
                    }

                    list.Add(new Tuple<bool, int, int>(isInput, uid, parentuid));
                }
            };

            Action<NodeBase> handler =
            (NodeBase node) =>
            {
                if (node is TreeNode)
                {
                    if (node is RootTreeNode)
                        return;

                    TreeMemory treeMemory = m_Tree.TreeMemory;

                    TreeNode treeNode = node as TreeNode;
                    int parentUID = treeNode.Parent == null ? -2 : (int)treeNode.Parent.UID;
                    foreach (var v in treeNode.NodeMemory.Datas)
                    {
                        processor(v.Variable, (int)node.UID, parentUID, v.Variable.IsInput);
                        if (v.Variable.IsElement)
                        {
                            processor(v.Variable.VectorIndex, (int)node.UID, parentUID, true);
                        }
                    }

                    if (treeNode is SubTreeNode)
                    {
                        foreach (var v in (treeNode as SubTreeNode).InOutMemory.InputMemory.Datas)
                        {
                            processor(v.Variable, (int)node.UID, parentUID, true);
                            if (v.Variable.IsElement)
                            {
                                processor(v.Variable.VectorIndex, (int)node.UID, parentUID, true);
                            }
                        }
                        foreach (var v in (treeNode as SubTreeNode).InOutMemory.OutputMemory.Datas)
                        {
                            processor(v.Variable, (int)node.UID, parentUID, false);
                            if (v.Variable.IsElement)
                            {
                                processor(v.Variable.VectorIndex, (int)node.UID, parentUID, true);
                            }
                        }
                    }
                }
            };


            //Utility.OperateNode(m_Tree.Root, handler);
            foreach (var tree in m_AllTrees)
            {
                Utility.OperateNode(tree, handler);
            }

            ///////> Check the meaning of IsInput of Variable
            ////foreach (var v in m_Tree.InOutMemory.InputMemory.Datas)
            ////{
            ////    processor(v.Variable, -1, false);
            ////    if (v.Variable.IsElement)
            ////    {
            ////        processor(v.Variable.VectorIndex, -1, true);
            ////    }
            ////}

            ///////> Check the meaning of IsInput of Variable
            ////foreach (var v in m_Tree.InOutMemory.OutputMemory.Datas)
            ////{
            ////    processor(v.Variable, 9999999, true);
            ////    if (v.Variable.IsElement)
            ////    {
            ////        processor(v.Variable.VectorIndex, 9999999, true);
            ////    }
            ////}

            foreach (var pair in res)
            {
                var list = pair.Value;
                /// Only Input or Output;
                /// When only input, the data may be from code
                /// When only output, it may be used in SubTree
                if (list.Count < 2)
                    continue;

                ///> It's very difficult to tell if the variable should be local when it's used
                ///  under different branch nodes.
                ///  We only check the variables used under the same Sequence/Selector/...
                int commonparent = -1;
                bool iscommon = true;
                foreach (var t in list)
                {
                    if (commonparent == -1)
                        commonparent = t.Item3;
                    else if (commonparent != t.Item3)
                    {
                        iscommon = false;
                        break;
                    }
                }

                if (!iscommon)
                    continue;

                list.Sort(
                    (Tuple<bool, int, int> a, Tuple<bool, int, int> b) => 
                {
                    if (a.Item2 == b.Item2)
                        return -a.Item1.CompareTo(b.Item1);
                    return a.Item2.CompareTo(b.Item2);
                }
                );
                /// We only check the first and the last
                /// If first output and last input, it has high chance of being local 
                if (!list[0].Item1 && list[list.Count - 1].Item1)
                {
                    Switch(pair.Key.Variable);
                }
            }
        }
    }
}
