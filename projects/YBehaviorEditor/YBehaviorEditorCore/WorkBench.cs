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

        public bool Load(XmlElement data)
        {
            foreach (XmlNode chi in data.ChildNodes)
            {
                if (chi.Name == "Node")
                {
                    var attr = chi.Attributes.GetNamedItem("Class");
                    if (attr == null)
                        continue;
                    if (attr.Value == "EntryTask")
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

                    node.Conns.AddNode(childNode, connectionIdentifier);
                    _LoadOneNode(childNode, chi);
                }
            }

            return true;
        }

    }
}
