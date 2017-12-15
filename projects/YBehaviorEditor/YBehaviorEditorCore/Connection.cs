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

        public bool AddNode(NodeBase node)
        {
            if (!_CanAdd(node))
                return false;

            m_Nodes.Add(node);
            node.ParentConn = this;
            m_Owner.Conns.MarkDirty();
            return true;
        }

        protected virtual bool _CanAdd(NodeBase node) { return true; }
    }

    public class Connections : System.Collections.IEnumerable
    {
        List<Connection> m_Connections = new List<Connection>();
        public System.Collections.IEnumerable ConnectionsList { get { return m_Connections; } }

        List<NodeBase> m_Nodes = new List<NodeBase>();
        public int NodeCount { get { _ProcessDirty(); return m_Nodes.Count; } }

        bool m_bDirty = true;
        public void MarkDirty()
        {
            m_bDirty = true;
        }

        public bool Add(Connection conn)
        {
            foreach (var c in m_Connections)
            {
                if (c.Identifier == conn.Identifier)
                    return false;
            }
            m_Connections.Add(conn);
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
                    foreach (NodeBase node in conn)
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
                if (conn.Identifier == identifier)
                    return conn;
            }
            return null;
        }

        public bool AddNode(NodeBase node, string identifier)
        {
            if (identifier == null)
                identifier = Connection.IdentifierChildren;
            Connection conn = GetConn(identifier);
            if (conn == null)
                return false;
            return conn.AddNode(node);
        }
    }

    ///////////////////////////////

    public class ConnectionNone : Connection
    {
        public ConnectionNone(NodeBase node, string identifier)
            : base(node, identifier)
        {
        }

        protected override bool _CanAdd(NodeBase node)
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

        protected override bool _CanAdd(NodeBase node)
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
