using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace YBehavior.Editor.Core
{
    public class WorkBench
    {
        List<Node> m_Forest = new List<Node>();
        public List<Node> Forest { get { return m_Forest; } }
        Tree m_Tree;
        public Tree MainTree { get { return m_Tree; } }

        public TreeFileMgr.TreeFileInfo FileInfo { get; set; }

        public WorkBench()
        {
            ///> TODO: these events should be removed when the bench is not active;
            EventMgr.Instance.Register(EventType.NodesConnected, _OnNodesConnected);
            EventMgr.Instance.Register(EventType.NodesDisconnected, _OnNodesDisconnected);
            EventMgr.Instance.Register(EventType.RemoveNode, _RemoveNode);
        }

        private void _OnNodesConnected(EventArg arg)
        {
            NodesConnectedArg oArg = arg as NodesConnectedArg;

            ConnectionHolder parent;
            ConnectionHolder child;

            if (ConnectionHolder.TryConnect(oArg.Holder0, oArg.Holder1, out parent, out child))
            {
                ///> refresh parent connections
                Node parentNode = parent.Owner as Node;
                parentNode.Renderer.RenderConnections();

                Node childNode = child.Owner as Node;
                RemoveSubTree(childNode);
            }
            else
            {
                ///> errorcode
            }

        }

        private void _OnNodesDisconnected(EventArg arg)
        {
            NodesDisconnectedArg oArg = arg as NodesDisconnectedArg;

            Connection conn = oArg.ChildHolder.Conn;
            if (conn == null)
                return;

            if (conn.RemoveNode(oArg.ChildHolder.Owner))
            {
                Node parentNode = conn.Owner as Node;
                parentNode.Renderer.RenderConnections();

                Node childNode = conn.Owner as Node;
                AddSubTree(childNode);

            }
            else
            {
                ///> errorcode
            }
        }

        private void _RemoveNode(EventArg arg)
        {
            RemoveNodeArg oArg = arg as RemoveNodeArg;

            RemoveSubTree(oArg.Node);
        }

        public void CreateEmptyRoot()
        {
            m_Tree = NodeMgr.Instance.CreateNodeByName("Root") as Tree;
        }

        public bool Load(XmlElement data)
        {
            foreach (XmlNode chi in data.ChildNodes)
            {
                if (chi.Name == "Node")
                {
                    var attr = chi.Attributes.GetNamedItem("Class");
                    if (attr == null)
                        continue;
                    if (attr.Value == "Root")
                    {
                        m_Tree = NodeMgr.Instance.CreateNodeByName("Root") as Tree;
                        _LoadTree(m_Tree, chi);
                    }
                    else
                    {
                        Node node = NodeMgr.Instance.CreateNodeByName(chi.Name);
                        if (node == null)
                        {
                            LogMgr.Instance.Error("Cant create node: " + chi.Name);
                            return false;
                        }
                        _LoadOneNode(node, chi);
                    }
                }
            }
            return true;
        }

        private bool _LoadTree(Tree tree, XmlNode data)
        {
            return _LoadOneNode(tree, data);
        }

        private bool _LoadOneNode(Node node, XmlNode data)
        {
            if (node == null)
                return false;

            node.Load(data);
            foreach (XmlNode chi in data.ChildNodes)
            {
                if (chi.Name == "Node")
                {
                    var attr = chi.Attributes.GetNamedItem("Class");
                    if (attr == null)
                        continue;
                    Node childNode = NodeMgr.Instance.CreateNodeByName(attr.Value);
                    if (childNode == null)
                    {
                        LogMgr.Instance.Error("Cant create node: " + chi.Name);
                        return false;
                    }

                    string connectionIdentifier = null;
                    attr = chi.Attributes.GetNamedItem("Connection");
                    if (attr != null)
                        connectionIdentifier = attr.Value;

                    node.Conns.Connect(childNode, connectionIdentifier);
                    _LoadOneNode(childNode, chi);
                }
            }

            return true;
        }

        public void Save(XmlElement data, XmlDocument xmlDoc)
        {
            _SaveNode(MainTree, data, xmlDoc);

            foreach (var tree in m_Forest)
            {
                _SaveNode(tree, data, xmlDoc);
            }
        }

        void _SaveNode(Node node, XmlElement data, XmlDocument xmlDoc)
        {
            XmlElement nodeEl = xmlDoc.CreateElement("Node");
            data.AppendChild(nodeEl);

            nodeEl.SetAttribute("Class", node.Name);
            node.Save(nodeEl);

            foreach (Node chi in node.Conns)
            {
                _SaveNode(chi, nodeEl, xmlDoc);
            }
        }

        public void RemoveSubTree(Node root)
        {
            if (root == null)
                return;
            for (int i = 0; i < m_Forest.Count; ++i)
            {
                if (m_Forest[i] == root)
                {
                    m_Forest.RemoveAt(i);
                    break;
                }
            }
        }

        public void AddSubTree(Node root)
        {
            if (root == null)
                return;
            m_Forest.Add(root);
        }
    }
}
