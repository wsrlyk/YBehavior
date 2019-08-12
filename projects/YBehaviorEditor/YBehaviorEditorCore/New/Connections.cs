﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace YBehavior.Editor.Core.New
{
    /// <summary>
    /// A line between two nodes(two holders)
    /// </summary>
    public class Connection
    {
        public Connector From { get; set; }
        public Connector To { get; set; }
        public Graph Graph { get; set; }

        public Connection(Connector from, Connector to)
        {
            From = from;
            To = to;
            //Graph = graph;
        }
    }

    public class ConnectorGeometry
    {
        public Connector Owner { get; set; }
        Point m_Pos;
        public Point Pos
        {
            get { return m_Pos; }
            set
            {
                if (m_Pos == value)
                    return;
                m_Pos = value;

                if (onPosChanged != null)
                    onPosChanged();
            }
        }

        double m_MidY;
        public double MidY
        {
            get { return m_MidY; }
            set
            {
                if (m_MidY == value)
                    return;

                m_MidY = value;
                if (onMidYChanged != null)
                    onMidYChanged();
            }
        }
        public Action onPosChanged;
        public Action onMidYChanged;
    }

    /// <summary>
    /// Hold the connections coming in or going out
    /// </summary>
    public class Connector
    {
        public enum Dir
        {
            IN,
            OUT,
        };

        public static readonly string IdentifierChildren = "children";
        public static readonly string IdentifierParent = "parent";
        public static readonly string IdentifierCondition = "condition";

        public static readonly string IdentifierDefault = "default";
        public static readonly string IdentifierIncrement = "inc";
        public static readonly string IdentifierInit = "init";
        public static readonly string IdentifierCond = "cond";

        protected string m_Identifier;
        public string Identifier { get { return m_Identifier; } }

        protected Dir m_Dir;
        public Dir GetDir => m_Dir;

        protected List<Connection> m_Conns = new List<Connection>();
        public List<Connection> Conns { get { return m_Conns; } }

        protected NodeBase m_Owner;
        public NodeBase Owner { get { return m_Owner; } }

        public ConnectorGeometry Geo { get { return m_Geo; } }
        ConnectorGeometry m_Geo;

        int m_AtBottom = 1;  //> Connections on the top of the nodes = -1;
        public Connector(NodeBase node, string identifier)
        {
            if (node == null)
                return;
            m_Owner = node;
            m_Identifier = identifier;

            m_Geo = new ConnectorGeometry()
            {
                Owner = this,
                onPosChanged = _OnPosChanged,
            };

            if (identifier == IdentifierCondition || identifier == IdentifierParent)
            {
                m_AtBottom = -1;
            }
        }

        public void RecalcMidY()
        {
            double childPos = CalcHorizontalPos();
            double parentPos = Geo.Pos.Y;
            double midPos;
            if (m_AtBottom > 0)
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

        void _OnPosChanged()
        {

        }

        public double CalcHorizontalPos()
        {
            double miny = double.MaxValue;
            foreach (Connection conn in m_Conns)
            {
                double top = conn.To.Geo.Pos.Y;
                miny = Math.Min(miny, top);
            }

            return miny;
        }

        protected virtual bool _CanAdd() { return true; }
        public bool Connect(Connector target)
        {
            if (target == null)
                return false;

            if (!_CanAdd())
                return false;
            if (!target._CanAdd())
                return false;

            foreach (var conn in target.Conns)
            {
                if (conn.From == this)
                {
                    ///> Already connected.
                    return false;
                }
            }

            Connection connection = new Connection(this, target);
            m_Conns.Add(connection);
            target.Conns.Add(connection);

            m_Owner.Conns.MarkDirty();


            RecalcMidY();

            target.Owner.OnConnectFromChanged();
            Owner.OnConnectToChanged();

            ///_CreateConnRenderer(target);
            return true;
        }

        //public static bool HasConnected(Connector left, Connector right)
        //{
        //    Connector smaller;
        //    Connector larger;;
        //    if (left.Conns.Count < right.Conns.Count)
        //    {
        //        smaller = left;
        //        larger = right;
        //    }
        //    else
        //    {
        //        smaller = right;
        //        larger = left;
        //    }

        //    foreach (var conn in smaller.Conns)
        //    {
        //        if ()
        //    }
        //}
        public static bool TryConnect(Connector left, Connector right, out Connector parent, out Connector child)
        {
            parent = null;
            child = null;

            if (left == null || right == null)
                return false;

            ///>From same owner. For now we dont support this.
            if (left.m_Owner == right.m_Owner)
                return false;

            if (left.GetDir == Dir.OUT)
                parent = left;
            else
                child = left;

            if (parent != null)
                child = right;
            else
                parent = right;

            if (parent.GetDir == Dir.IN || child.GetDir == Dir.OUT)
                return false;

            return parent.Connect(child);
        }

        public static bool TryDisconnect(Connection connection, out Connector parent, out Connector child)
        {
            parent = null;
            child = null;

            if (connection == null)
                return false;

            parent = connection.From;
            child = connection.To;

            parent.Conns.Remove(connection);
            child.Conns.Remove(connection);


            parent.Owner.Conns.MarkDirty();
            parent.RecalcMidY();

            child.Owner.OnConnectFromChanged();
            parent.Owner.OnConnectToChanged();

            return true;
        }
    }


    public class Connections : System.Collections.IEnumerable
    {
        NodeBase m_Owner;
        List<Connector> m_Connectors = new List<Connector>();
        public System.Collections.IEnumerable ConnectorsList { get { return m_Connectors; } }

        Connector m_ParentConnector;
        public Connector ParentConnector { get { return m_ParentConnector; } }

        List<NodeBase> m_Nodes = new List<NodeBase>();
        public int NodeCount { get { _ProcessDirty(); return m_Nodes.Count; } }

        bool m_bDirty = true;
        public void MarkDirty()
        {
            m_bDirty = true;
        }

        public Connections(NodeBase owner)
        {
            m_Owner = owner;
        }

        public void CreateParentHolder()
        {
            if (m_ParentConnector == null)
                m_ParentConnector = new Connector(m_Owner, Connector.IdentifierParent);
        }

        public bool Add(string identifier)
        {
            if (identifier == Connector.IdentifierParent)
            {
                if (m_ParentConnector == null)
                    m_ParentConnector = new Connector(m_Owner, Connector.IdentifierParent);
                else
                    return false;
            }
            else
            {
                foreach (var c in m_Connectors)
                {
                    if (c.Identifier == identifier)
                        return false;
                }
                m_Connectors.Add(new Connector(m_Owner, identifier));
            }
            return true;
        }

        void _ProcessDirty()
        {
            if (m_bDirty)
            {
                m_bDirty = false;
                m_Nodes.Clear();
                foreach (var ctrs in m_Connectors)
                {
                    foreach (var conn in ctrs.Conns)
                    {
                        m_Nodes.Add(conn.To.Owner);
                    }
                }
            }
        }
        public System.Collections.IEnumerator GetEnumerator()
        {
            _ProcessDirty();
            return m_Nodes.GetEnumerator();
        }

        public Connector GetConnector(string identifier)
        {
            if (identifier == Connector.IdentifierParent)
            {
                if (m_ParentConnector != null)
                    return m_ParentConnector;
                else
                    return null;
            }

            foreach (var conn in m_Connectors)
            {
                if (conn.Identifier == identifier)
                    return conn;
            }

            return null;
        }

        public bool Connect(NodeBase target, string identifier)
        {
            if (identifier == null)
                identifier = Connector.IdentifierChildren;
            Connector conn = GetConnector(identifier);
            if (conn == null)
                return false;
            return conn.Connect(target.Conns.ParentConnector);
        }

        public bool Connect(Connector target, string identifier)
        {
            if (identifier == null)
                identifier = Connector.IdentifierChildren;
            Connector conn = GetConnector(identifier);
            if (conn == null)
                return false;
            return conn.Connect(target);
        }

        public void Sort(Comparison<Connection> comparer)
        {
            foreach (var c in m_Connectors)
            {
                c.Conns.Sort(comparer);
            }
        }
    }

    ///////////////////////////////


    public class ConnectorSingle : Connector
    {
        public ConnectorSingle(NodeBase node, string identifier)
            : base(node, identifier)
        {
        }

        protected override bool _CanAdd()
        {
            return m_Conns.Count == 0;
        }
    }

    public class ConnectorMultiple : Connector
    {
        public ConnectorMultiple(NodeBase node, string identifier)
            : base(node, identifier)
        {
        }
    }
}
