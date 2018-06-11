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
        public CommandMgr CommandMgr { get; } = new CommandMgr();

        public DelayableNotificationCollection<Comment> Comments { get; } = new DelayableNotificationCollection<Comment>();
        public DelayableNotificationCollection<Renderer> NodeList { get; } = new DelayableNotificationCollection<Renderer>();
        public DelayableNotificationCollection<ConnectionRenderer> ConnectionList { get; } = new DelayableNotificationCollection<ConnectionRenderer>();

        public string FilePath { get; set; }

        private TreeFileMgr.TreeFileInfo m_UntitledFileInfo = null;
        public TreeFileMgr.TreeFileInfo FileInfo
        {
            get
            {
                return string.IsNullOrEmpty(FilePath) ? m_UntitledFileInfo : TreeFileMgr.Instance.GetFileInfo(FilePath);
            }
            set
            {
                m_UntitledFileInfo = value;
            }
        }
        static int g_ID_inc = 0;

        int m_UID;
        public WorkBench()
        {
            m_UID = ++g_ID_inc;
        }

        public void ConnectNodes(ConnectionHolder holder0, ConnectionHolder holder1)
        {
            ConnectionHolder parent;
            ConnectionHolder child;

            if (ConnectionHolder.TryConnect(holder0, holder1, out parent, out child))
            {
                ///> refresh parent connections
                //Node parentNode = parent.Owner as Node;
                //parentNode.Renderer.CreateConnections();
                ConnectionRenderer connRenderer = child.Conn.GetConnectionRenderer(child.Owner);
                if (connRenderer != null)
                    ConnectionList.Add(connRenderer);

                Node childNode = child.Owner as Node;
                RemoveForestTree(childNode, false);

                RefreshNodeUID();

                ConnectNodeCommand connectNodeCommand = new ConnectNodeCommand
                {
                    Parent = parent,
                    Child = child
                };
                CommandMgr.PushDoneCommand(connectNodeCommand);
            }
            else
            {
                ///> errorcode
            }

        }

        public void DisconnectNodes(ConnectionHolder childHolder)
        {
            Connection conn = childHolder.Conn;
            if (conn == null)
                return;
            ConnectionRenderer connRenderer = conn.GetConnectionRenderer(childHolder.Owner);

            if (conn.RemoveNode(childHolder.Owner))
            {
                if (connRenderer != null)
                    ConnectionList.Remove(connRenderer);
                //Node parentNode = conn.Owner as Node;
                //parentNode.Renderer.CreateConnections();

                Node childNode = childHolder.Owner as Node;
                AddForestTree(childNode, false);

                RefreshNodeUID();

                DisconnectNodeCommand disconnectNodeCommand = new DisconnectNodeCommand
                {
                    Parent = conn.Holder,
                    Child = childHolder
                };
                CommandMgr.PushDoneCommand(disconnectNodeCommand);
            }
            else
            {
                ///> errorcode
            }
        }

        public void RemoveNode(Node node)
        {
            RemoveForestTree(node);

            RemoveNodeCommand removeNodeCommand = new RemoveNodeCommand()
            {
                Node = node
            };
            CommandMgr.PushDoneCommand(removeNodeCommand);
        }

        public void AddNode(Node node)
        {
            AddForestTree(node);

            AddNodeCommand addNodeCommand = new AddNodeCommand()
            {
                Node = node
            };
            CommandMgr.PushDoneCommand(addNodeCommand);
        }

        public void OnNodeMoved(EventArg arg)
        {
            NodeMovedArg oArg = arg as NodeMovedArg;
            Node parent = oArg.Node.Parent as Node;
            if (parent != null)
            {
                _RefreshNodeUIDFromMiddle(parent);
            }
        }

        public void RemoveComment(Comment comment)
        {
            if (comment != null)
                Comments.Remove(comment);

            RemoveCommentCommand removeCommentCommand = new RemoveCommentCommand()
            {
                Comment = comment
            };
            CommandMgr.PushDoneCommand(removeCommentCommand);
        }

        public void AddComment(Comment comment)
        {
            if (comment != null)
                Comments.Add(comment);

            AddCommentCommand addCommentCommand = new AddCommentCommand()
            {
                Comment = comment
            };
            CommandMgr.PushDoneCommand(addCommentCommand);
        }

        public void CreateComment()
        {
            Comment comment = new Comment();
            AddComment(comment);

            CommentCreatedArg cArg = new CommentCreatedArg()
            {
                Comment = comment
            };
            EventMgr.Instance.Send(cArg);
        }

        public void CreateEmptyRoot()
        {
            m_Tree = NodeMgr.Instance.CreateNodeByName("Root") as Tree;
        }

        public bool Load(XmlElement data)
        {
            CommandMgr.Blocked = true;

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
                        AddRenderers(m_Tree);
                    }
                    else
                    {
                        Node node = NodeMgr.Instance.CreateNodeByName(attr.Value);
                        if (node == null)
                        {
                            LogMgr.Instance.Error("Cant create node: " + attr.Value);
                            return false;
                        }
                        _LoadTree(node, chi);
                        AddForestTree(node);
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

        private bool _LoadTree(Node tree, XmlNode data)
        {
            bool bRes = _LoadOneNode(tree, data);
            Utility.InitNode(tree, true);
            RefreshNodeUID();
            //tree.Renderer.CreateConnections();
            return bRes;
        }

        private bool _LoadComments(XmlNode root)
        {
            Comments.Clear();
            foreach (XmlNode chi in root.ChildNodes)
            {
                if (chi.Name == "Comment")
                {
                    Comment comment = new Comment();
                    //var attr = chi.Attributes.GetNamedItem("Title");
                    //if (attr != null)
                    //    comment.Name = attr.Value;
                    var attr = chi.Attributes.GetNamedItem("Content");
                    if (attr != null)
                        comment.Content = attr.Value;
                    attr = chi.Attributes.GetNamedItem("Rect");
                    if (attr != null)
                        comment.Geo.Rec = System.Windows.Rect.Parse(attr.Value);

                    Comments.Add(comment);
                }
            }
            return true;
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
            node.Conns.Sort(Node.SortByPosX);
            if (node.HasConditionConnection)
                node.EnableCondition = true;
            return true;
        }

        public void Save(XmlElement data, XmlDocument xmlDoc)
        {
            _SaveNode(MainTree, data, xmlDoc);

            foreach (var tree in m_Forest)
            {
                _SaveNode(tree, data, xmlDoc);
            }

            _SaveComments(data, xmlDoc);
            RefreshNodeUID();

            CommandMgr.Dirty = false;
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

        void _SaveComments(XmlElement parent, XmlDocument xmlDoc)
        {
            if (Comments.Count > 0)
            {
                XmlElement root = xmlDoc.CreateElement("Comments");
                parent.AppendChild(root);

                foreach (Comment comment in Comments)
                {
                    XmlElement comEl = xmlDoc.CreateElement("Comment");
                    //comEl.SetAttribute("Title", comment.Name);
                    comEl.SetAttribute("Content", comment.Content);
                    comEl.SetAttribute("Rect", comment.Geo.Rec.ToString());
                    root.AppendChild(comEl);
                }
            }
        }

        public void Export(XmlElement data, XmlDocument xmlDoc)
        {
            _ExportNode(MainTree, data, xmlDoc);

            RefreshNodeUID();
        }

        void _ExportNode(Node node, XmlElement data, XmlDocument xmlDoc)
        {
            if (node.Disabled)
                return;

            XmlElement nodeEl = xmlDoc.CreateElement("Node");
            data.AppendChild(nodeEl);

            nodeEl.SetAttribute("Class", node.Name);
            node.Export(nodeEl);

            foreach (Node chi in node.Conns)
            {
                _ExportNode(chi, nodeEl, xmlDoc);
            }
        }

        public void RefreshNodeUID()
        {
            _RefreshNodeUIDFromRoot(MainTree);
        }

        void _RefreshNodeUIDFromRoot(Node node)
        {
            uint uid = 0;
            _RefreshNodeUID(node, ref uid);
        }

        void _RefreshNodeUIDFromMiddle(Node node)
        {
            uint uid = node.UID - 1;
            _RefreshNodeUID(node, ref uid);
        }

        void _RefreshNodeUID(Node node, ref uint uid)
        {
            if (node.Disabled)
                node.UID = 0;
            else
                node.UID = ++uid;

            foreach (Node chi in node.Conns)
            {
                _RefreshNodeUID(chi, ref uid);
            }
        }

        public void RemoveForestTree(Node root, bool bRemoveRenderer = true)
        {
            if (root == null)
                return;

            if (bRemoveRenderer)
                RemoveRenderers(root);

            m_Forest.Remove(root);
        }

        public void AddForestTree(Node root, bool bAddRenderer = true)
        {
            if (root == null)
                return;
            m_Forest.Add(root);

            if (bAddRenderer)
                AddRenderers(root);

            _RefreshNodeUIDFromRoot(root);
        }

        public void AddRenderers(Node node)
        {
            using (var v1 = ConnectionList.Delay())
            {
                using (var v2 = NodeList.Delay())
                {
                    _AddRenderers(node);
                }
            }
        }

        void _AddRenderers(Node node)
        {
            NodeList.Add(node.Renderer);

            foreach (Node chi in node.Conns)
            {
                _AddRenderers(chi);
            }

            foreach (ConnectionHolder conn in node.Conns.ConnectionsList)
            {
                foreach (ConnectionRenderer renderer in conn.Conn.Renderers)
                {
                    ConnectionList.Add(renderer);
                }
            }
        }

        public void RemoveRenderers(Node node)
        {
            //using (var v1 = ConnectionList.Delay())
            {
                //using (var v2 = NodeList.Delay())
                {
                    _RemoveRenderers(node);
                }
            }
        }

        void _RemoveRenderers(Node node)
        {
            foreach (ConnectionHolder conn in node.Conns.ConnectionsList)
            {
                foreach (ConnectionRenderer renderer in conn.Conn.Renderers)
                {
                    ConnectionList.Remove(renderer);
                }
            }

            foreach (Node chi in node.Conns)
            {
                _RemoveRenderers(chi);
            }

            NodeList.Remove(node.Renderer);
        }

        public bool CheckError()
        {
            return _CheckError(m_Tree);
        }

        private bool _CheckError(Node node)
        {
            bool bRes = true;
            if (!node.CheckValid())
                bRes = false;
            foreach (Variable v in node.Variables.Datas.Values)
            {
                if (!v.CheckValid())
                {
                    LogMgr.Instance.Error("CheckError in Node: " + node.UITitle + ", Variable: " + v.Name);
                    bRes = false;
                }
            }

            foreach (Node child in node.Conns)
            {
                if (!_CheckError(child))
                    bRes = false;
            }
            return bRes;
        }
    }
}
