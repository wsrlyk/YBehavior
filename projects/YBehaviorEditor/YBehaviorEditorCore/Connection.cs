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

        protected NodeBase m_Owner;
        public NodeBase Owner { get { return m_Owner; } }
        protected List<NodeBase> m_Nodes = new List<NodeBase>();
        protected string m_Identifier;
        public string Identifier { get { return m_Identifier; } }

        public Connection(NodeBase node, string identifier)
        {
            m_Owner = node;
            node.Conns.Add(this);
            m_Identifier = identifier;
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

            target.OnParentChanged();
            Owner.OnChildChanged();
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
                    m_Nodes.RemoveAt(i);
                    m_Owner.Conns.MarkDirty();

                    target.Conns.ParentHolder.SetConn(null);

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
        }

        static int CompareByPosX(Node a, Node b)
        {
            return 0;
        }

        protected virtual bool _CanAdd() { return true; }
    }

    public class ConnectionHolder
    {
        Connection m_Conn;
        public Connection Conn { get { return m_Conn; } }

        protected NodeBase m_Owner;
        public NodeBase Owner { get { return m_Owner; } }

        public ConnectionHolder(Connection conn)
        {
            if (conn == null)
                return;
            m_Conn = conn;
            m_Owner = conn.Owner;
        }

        public ConnectionHolder(NodeBase owner)
        {
            m_Owner = owner;
        }

        public void SetConn(Connection conn)
        {
            m_Conn = conn;
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
            foreach (var conn in m_Connections)
            {
                if (conn.Conn.Identifier == identifier)
                    return conn.Conn;
            }
            if (identifier == Connection.IdentifierParent && m_ParentHolder != null)
                return m_ParentHolder.Conn;

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
