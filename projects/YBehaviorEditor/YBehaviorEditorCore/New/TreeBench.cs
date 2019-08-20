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
            m_Tree.RefreshNodeUID();
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

                m_Tree.RefreshNodeUID();

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

                m_Tree.RefreshNodeUID();

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

        public override bool Load(XmlElement data)
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
                        AddRenderers(m_Tree.Root, false);
                    }
                    else
                    {
                        TreeNode node = TreeNodeMgr.Instance.CreateNodeByName(attr.Value);
                        if (node == null)
                        {
                            LogMgr.Instance.Error("Cant create node: " + attr.Value);
                            return false;
                        }
                        _LoadTree(node, chi);
                        AddForestTree(node, true);
                    }
                }
                else if (chi.Name == "Comments")
                {
                    _LoadComments(chi);
                }
            }

            CommandMgr.Blocked = false;
            return true;
        }

        private bool _LoadTree(TreeNode tree, XmlNode data)
        {
            m_Tree.SetFlag(Graph.FLAG_LOADING);
            bool bRes = _LoadOneNode(tree, data);
            m_Tree.RemoveFlag(Graph.FLAG_LOADING);
            ////Utility.InitNode(tree, true);
            m_Tree.RefreshNodeUID();
            return bRes;
        }

        private bool _LoadOneNode(TreeNode node, XmlNode data)
        {
            if (node == null)
                return false;

            node.Load(data);
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
                        LogMgr.Instance.Error("Cant create node: " + attr.Value);
                        return false;
                    }

                    string connectionIdentifier = null;
                    attr = chi.Attributes["Connection"];
                    if (attr != null)
                        connectionIdentifier = attr.Value;

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

        public override void Save(XmlElement data, XmlDocument xmlDoc) 
        {
            _SaveNode(m_Tree.Root, data, xmlDoc);

            foreach (var tree in m_Forest)
            {
                _SaveNode(tree, data, xmlDoc);
            }

            _SaveComments(data, xmlDoc);
            m_Tree.RefreshNodeUID();

            CommandMgr.Dirty = false;
            m_ExportFileHash = 0;

            OnPropertyChanged("DisplayName");
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
            m_Tree.RefreshNodeUID();
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

            m_Tree.RefreshNodeUIDFromRoot(root);
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
    }
}
