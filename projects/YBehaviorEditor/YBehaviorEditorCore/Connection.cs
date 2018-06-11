using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace YBehavior.Editor.Core
{
    public class Connection : System.Collections.IEnumerable
    {
        public static readonly string IdentifierChildren = "children";
        public static readonly string IdentifierParent = "parent";
        public static readonly string IdentifierCondition = "condition";

        public ConnectionHolder Holder { get; set; }
        protected NodeBase m_Owner;
        public NodeBase Owner { get { return m_Owner; } }
        protected List<NodeBase> m_Nodes = new List<NodeBase>();
        protected string m_Identifier;
        public string Identifier { get { return m_Identifier; } }

        Dictionary<NodeBase, ConnectionRenderer> m_ConnectionRenderers = new Dictionary<NodeBase, ConnectionRenderer>();
        public System.Collections.IEnumerable Renderers { get { return m_ConnectionRenderers.Values; } }

        public ConnectionRenderer GetConnectionRenderer(NodeBase node)
        {
            if (m_ConnectionRenderers.TryGetValue(node, out ConnectionRenderer renderer))
            {
                return renderer;
            }
            return null;
        }

        protected void _CreateConnRenderer(NodeBase child)
        {
            ConnectionRenderer connectionRenderer = new ConnectionRenderer();
            _SetConnRenderer(connectionRenderer, child);
            m_ConnectionRenderers.Add(child, connectionRenderer);
            //RenderMgr.Instance.AddConnection(connectionRenderer);
        }
        protected void _RemoveConnRenderer(NodeBase child)
        {
            if (m_ConnectionRenderers.TryGetValue(child, out ConnectionRenderer connectionRenderer))
            {
                //RenderMgr.Instance.RemoveConnection(connectionRenderer);
                connectionRenderer.Destroy();
                m_ConnectionRenderers.Remove(child);
            }
        }

        protected void _SetConnRenderer(ConnectionRenderer connectionRenderer, NodeBase child)
        {
            connectionRenderer.ParentConnectorGeo = this.Holder.Geo;
            connectionRenderer.ChildConnectorGeo = child.Conns.ParentHolder.Geo;
            connectionRenderer.ChildConn = child.Conns.ParentHolder;
        }


        public Connection(NodeBase node, string identifier)
        {
            m_Owner = node;
            m_Identifier = identifier;
            node.Conns.Add(this);
        }

        public System.Collections.IEnumerator GetEnumerator()
        {
            return m_Nodes.GetEnumerator();
        }

        public int NodeCount { get { return m_Nodes.Count; } }

        public bool AddNode(NodeBase target)
        {
            if (target == null)
                return false;

            if (target.Conns.ParentHolder == null)
                return false;

            if (!_CanAdd())
                return false;
            
            m_Nodes.Add(target);
            m_Owner.Conns.MarkDirty();


            target.Conns.ParentHolder.SetConn(this);
            this.Holder.RecalcMidY();

            target.OnParentChanged();
            Owner.OnChildChanged();

            _CreateConnRenderer(target);
            return true;
        }

        public bool RemoveNode(NodeBase target)
        {
            if (target == null)
                return false;

            if (target.Conns.ParentHolder == null || target.Conns.ParentHolder.Conn != this)
                return false;

            for (int i = 0; i < m_Nodes.Count; ++i)
            {
                if (m_Nodes[i] == target)
                {
                    _RemoveConnRenderer(target);

                    m_Nodes.RemoveAt(i);
                    m_Owner.Conns.MarkDirty();

                    target.Conns.ParentHolder.SetConn(null);
                    this.Holder.RecalcMidY();
                    target.OnParentChanged();
                    Owner.OnChildChanged();

                    return true;
                }
            }

            return false;
        }

        public void Sort(Comparison<NodeBase> comparer)
        {
            m_Nodes.Sort(comparer);
            m_Owner.Conns.MarkDirty();
        }

        static int CompareByPosX(Node a, Node b)
        {
            return 0;
        }

        protected virtual bool _CanAdd() { return true; }

        public double CalcHorizontalPos()
        {
            //double y = 0;
            double miny = double.MaxValue;
            foreach (Node child in m_Nodes)
            {
                double top = child.Conns.ParentHolder.Geo.Pos.Y;
                //y += top;
                miny = Math.Min(miny, top);
            }
            //y /= NodeCount;
            //y -= 20;
            //miny -= 20;
            //y = Math.Min(miny, y);
            //y = Math.Max(y, Holder.Geo.Pos.Y + /*Frame.ActualHeight + */10);
            return miny;
        }
    }

    public class ConnectionHolder
    {
        Connection m_Conn;
        public Connection Conn { get { return m_Conn; } }

        protected NodeBase m_Owner;
        public NodeBase Owner { get { return m_Owner; } }

        public ConnectorGeometry Geo { get { return m_Geo; } }
        ConnectorGeometry m_Geo;

        int m_Dir = 1;  //> Connections on the top of the nodes = -1;
        public ConnectionHolder(Connection conn)
        {
            if (conn == null)
                return;
            m_Conn = conn;
            m_Owner = conn.Owner;

            m_Geo = new ConnectorGeometry()
            {
                Holder = this,
                Identifier = conn.Identifier,
                onPosChanged = _OnChildConnectorChanged,
            };

            if (conn.Identifier == Connection.IdentifierCondition)
            {
                m_Dir = -1;
            }

            conn.Holder = this;
        }

        /// <summary>
        /// Virtual Holder for connection to the parent
        /// </summary>
        /// <param name="owner"></param>
        public ConnectionHolder(NodeBase owner)
        {
            m_Owner = owner;
            m_Geo = new ConnectorGeometry()
            {
                Holder = this,
                Identifier = Connection.IdentifierParent,
                onPosChanged = _OnParentConnectorChanged,
            };
            m_Dir = -1;
        }

        public void SetConn(Connection conn)
        {
            m_Conn = conn;
        }

        private void _OnParentConnectorChanged()
        {
            if (m_Conn != null)
                m_Conn.Holder._OnChildConnectorChanged();
        }


        private void _OnChildConnectorChanged()
        {
            RecalcMidY();
        }

        public void RecalcMidY()
        {
            double childPos = m_Conn.CalcHorizontalPos();
            double parentPos = Geo.Pos.Y;
            double midPos;
            if (m_Dir > 0)
            {
                if (childPos <= parentPos)
                    midPos = parentPos;
                else
                    midPos = parentPos + (childPos - parentPos) * 0.666;
            }
            else
            {
                midPos = Math.Min(childPos, parentPos);
            }
            m_Geo.MidY = midPos;
        }

        public static bool TryConnect(ConnectionHolder left, ConnectionHolder right, out ConnectionHolder parent, out ConnectionHolder child)
        {
            parent = null;
            child = null;

            if (left == null || right == null)
                return false;

            ///> From same owner
            if (left.m_Owner == right.m_Owner)
                return false;

            if (left.m_Conn != null)
                parent = left;
            else
                child = left;

            if (parent != null)
                child = right;
            else
                parent = right;

            ///> Parent has no self conn
            if (parent.m_Conn == null || parent.Conn.Owner != parent.Owner)
                return false;

            ///> Child already has a conn
            if (child.m_Conn != null)
                return false;

            return parent.Conn.AddNode(child.Owner);
        }
    }

    public class Connections : System.Collections.IEnumerable
    {
        List<ConnectionHolder> m_Connections = new List<ConnectionHolder>();
        public System.Collections.IEnumerable ConnectionsList { get { return m_Connections; } }

        ConnectionHolder m_ParentHolder;
        public ConnectionHolder ParentHolder { get { return m_ParentHolder; } }

        List<NodeBase> m_Nodes = new List<NodeBase>();
        public int NodeCount { get { _ProcessDirty(); return m_Nodes.Count; } }

        bool m_bDirty = true;
        public void MarkDirty()
        {
            m_bDirty = true;
        }

        public void CreateParentHolder(NodeBase owner)
        {
            if (m_ParentHolder == null)
                m_ParentHolder = new ConnectionHolder(owner);
        }

        public bool Add(Connection conn)
        {
            foreach (var c in m_Connections)
            {
                if (c.Conn.Identifier == conn.Identifier)
                    return false;
            }
            m_Connections.Add(new ConnectionHolder(conn));
            MarkDirty();
            return true;
        }

        void _ProcessDirty()
        {
            if (m_bDirty)
            {
                m_bDirty = false;
                m_Nodes.Clear();
                foreach (var conn in m_Connections)
                {
                    foreach (NodeBase node in conn.Conn)
                    {
                        m_Nodes.Add(node);
                    }
                }
            }
        }
        public System.Collections.IEnumerator GetEnumerator()
        {
            _ProcessDirty();
            return m_Nodes.GetEnumerator();
        }

        public Connection GetConn(string identifier)
        {
            ConnectionHolder holder = GetConnHolder(identifier);
            if (holder != null)
                return holder.Conn;
            return null;
        }
        public ConnectionHolder GetConnHolder(string identifier)
        {
            if (identifier == Connection.IdentifierParent && m_ParentHolder != null)
                return m_ParentHolder;

            foreach (var conn in m_Connections)
            {
                if (conn.Conn.Identifier == identifier)
                    return conn;
            }

            return null;
        }

        public bool Connect(NodeBase target, string identifier)
        {
            if (identifier == null)
                identifier = Connection.IdentifierChildren;
            Connection conn = GetConn(identifier);
            if (conn == null)
                return false;
            return conn.AddNode(target);
        }

        public void Sort(Comparison<NodeBase> comparer)
        {
            foreach (var c in m_Connections)
            {
                c.Conn.Sort(comparer);
            }
        }
    }

    ///////////////////////////////

    public class ConnectionNone : Connection
    {
        public ConnectionNone(NodeBase node, string identifier)
            : base(node, identifier)
        {
        }

        protected override bool _CanAdd()
        {
            return false;
        }
    }
    public class ConnectionSingle : Connection
    {
        public ConnectionSingle(NodeBase node, string identifier)
            : base(node, identifier)
        {
        }

        protected override bool _CanAdd()
        {
            return m_Nodes == null || m_Nodes.Count == 0;
        }
    }

    public class ConnectionMultiple : Connection
    {
        public ConnectionMultiple(NodeBase node, string identifier)
            : base(node, identifier)
        {
        }
    }
}
