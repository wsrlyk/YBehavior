using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace YBehavior.Editor.Core.New
{
    public class TreeBench : WorkBench
    {
        /// <summary>
        /// This will not be decreased when removing a tree in the forest.
        /// Just for getting a proper start uid for the forest.
        /// </summary>
        uint m_TotalForestCount = 0;
        List<TreeNode> m_Forest = new List<TreeNode>();
        public List<TreeNode> Forest { get { return m_Forest; } }

        Tree m_Tree;
        public Tree Tree { get { return m_Tree; } }
        public TreeBench()
        {
            m_Tree = new Tree();
            m_Graph = m_Tree;
        }

        public override void InitEmpty()
        {
            Utility.OperateNode(m_Tree.Root, m_Graph, false, NodeBase.OnAddToGraph);
            m_Tree.RefreshNodeUID(0);
            AddRenderers(m_Tree.Root, true);
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

                TreeNode childNode = child.Owner as TreeNode;
                RemoveForestTree(childNode, false);

                m_Tree.RefreshNodeUID(0);

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

                TreeNode childNode = connection.To.Owner as TreeNode;
                AddForestTree(childNode, false);

                m_Tree.RefreshNodeUID(0);
                _RefreshForestUID(childNode);

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
            Utility.OperateNode(node, m_Graph, true, NodeBase.OnAddToGraph);
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
                else if (chi.Name == "Comments")
                {
                    _LoadComments(chi);
                }
            }

            RefreshReferenceStates();
            CommandMgr.Blocked = false;
            return true;
        }

        void _LoadSuo(TreeNode tree)
        {
            string fileName = this.FileInfo.RelativeName;
            var map = Config.Instance.Suo.GetDebugPointInfo(fileName);
            if (map != null)
            {
                Action<NodeBase> action = (NodeBase node) =>
                {
                    node.DebugPointInfo.HitCount = map.GetDebugPoint(node.UID);
                    if (map.GetFold(node.UID))
                        (node as TreeNode).Folded = true;
                };
                Utility.OperateNode(tree, true, action);
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
            Utility.OperateNode(node, m_Graph, false, NodeBase.OnAddToGraph);

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

                    string connectionIdentifier = null;
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

            Utility.OperateNode(m_Tree.Root, true, func);
            m_TotalForestCount = 0;
            foreach (var tree in m_Forest)
            {
                _RefreshForestUID(tree);
                Utility.OperateNode(tree, true, func);
            }
        }

        public override void Save(XmlElement data, XmlDocument xmlDoc) 
        {
            SaveSuo();
            _SaveNode(m_Tree.Root, data, xmlDoc);

            foreach (var tree in m_Forest)
            {
                _SaveNode(tree, data, xmlDoc);
            }

            _SaveComments(data, xmlDoc);
            m_Tree.RefreshNodeUID(0);

            CommandMgr.Dirty = false;
            m_ExportFileHash = 0;

            OnPropertyChanged("DisplayName");

            RefreshReferenceStates();
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

        public override void Export(XmlElement data, XmlDocument xmlDoc)
        {
            _ExportNode(m_Tree.Root, data, xmlDoc);

            m_ExportFileHash = GenerateHash(data.OuterXml.Replace(" ", string.Empty));
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

        public void RemoveForestTree(TreeNode root, bool bRemoveRenderer = true)
        {
            if (root == null)
                return;

            if (bRemoveRenderer)
                RemoveRenderers(root);

            m_Forest.Remove(root);
        }

        public void AddForestTree(TreeNode root, bool bAddRenderer)
        {
            if (root == null)
                return;
            m_Forest.Add(root);

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

            foreach (Connector ctr in node.Conns.ConnectorsList)
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
            foreach (Connector ctr in node.Conns.ConnectorsList)
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
            return _CheckError(m_Tree.Root);
        }

        private bool _CheckError(TreeNode node)
        {
            bool bRes = true;
            if (!node.CheckValid())
                bRes = false;
            foreach (VariableHolder v in node.Variables.Datas)
            {
                if (!v.Variable.CheckValid())
                {
                    LogMgr.Instance.Error("CheckError in Node: " + node.Renderer.UITitle + ", Variable: " + v.Variable.Name);
                    bRes = false;
                }
            }

            foreach (TreeNode child in node.Conns)
            {
                if (!_CheckError(child))
                    bRes = false;
            }
            return bRes;
        }


        public void RefreshReferenceStates()
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

            Action<NodeBase, object, object> handler =
            (NodeBase node, object o0, object o1) =>
            {
                if (node is TreeNode)
                {
                    if (node is RootTreeNode)
                        return;

                    Tree tree = o0 as Tree;
                    if (tree == null)
                        return;

                    TreeMemory treeMemory = tree.TreeMemory;

                    bool isMainTree = (bool)o1;


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

            Utility.OperateNode(m_Tree.Root, m_Tree, true, true, handler);
            foreach (var tree in m_Forest)
            {
                Utility.OperateNode(tree, m_Tree, false, true, handler);
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

        public void Switch(Variable v)
        {
            if (m_Tree.SharedData.SwitchVariable(v))
            {
                RefreshAfterSwitchVariable(v);
                LogMgr.Instance.Log("Switch " + v.DisplayName);
            }
        }

        public bool RefreshAfterSwitchVariable(Variable variable)
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

            Utility.OperateNode(m_Tree.Root, true, handler);
            foreach (var tree in m_Forest)
            {
                Utility.OperateNode(tree, true, handler);
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
                    foreach (var v in treeNode.NodeMemory.Datas)
                    {
                        processor(v.Variable, (int)node.UID, (int)treeNode.Parent.UID, v.Variable.IsInput);
                        if (v.Variable.IsElement)
                        {
                            processor(v.Variable.VectorIndex, (int)node.UID, (int)treeNode.Parent.UID, true);
                        }
                    }

                    if (treeNode is SubTreeNode)
                    {
                        foreach (var v in (treeNode as SubTreeNode).InOutMemory.InputMemory.Datas)
                        {
                            processor(v.Variable, (int)node.UID, (int)treeNode.Parent.UID, true);
                            if (v.Variable.IsElement)
                            {
                                processor(v.Variable.VectorIndex, (int)node.UID, (int)treeNode.Parent.UID, true);
                            }
                        }
                        foreach (var v in (treeNode as SubTreeNode).InOutMemory.OutputMemory.Datas)
                        {
                            processor(v.Variable, (int)node.UID, (int)treeNode.Parent.UID, false);
                            if (v.Variable.IsElement)
                            {
                                processor(v.Variable.VectorIndex, (int)node.UID, (int)treeNode.Parent.UID, true);
                            }
                        }
                    }
                }
            };


            Utility.OperateNode(m_Tree.Root, true, handler);
            foreach (var tree in m_Forest)
            {
                Utility.OperateNode(tree, true, handler);
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
