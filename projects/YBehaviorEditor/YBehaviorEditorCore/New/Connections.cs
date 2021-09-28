using System;
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
        public struct FromTo : IEquatable<FromTo>
        {
            public Connector From { get; set; }
            public Connector To { get; set; }

            public bool Equals(FromTo other)
            {
                return From == other.From && To == other.To;
            }
        }
        public FromTo Ctr { get; set; }

        private string m_Note = string.Empty;
        public string Note
        {
            get { return m_Note; }
            set
            {
                if (m_Note == value)
                    return;
                m_Note = value;
                Renderer.OnPropertyChanged("Note");
            }
        }
        public ConnectionRenderer Renderer { get; set; }

        public Connection(Connector from, Connector to)
        {
            FromTo ctr = new FromTo();
            ctr.From = from;
            ctr.To = to;
            Ctr = ctr;

            //Graph = graph;
            Renderer = _CreateRenderer(from.IsVertical);
            Renderer.Owner = this;
            Renderer.ParentConnectorGeo = from.Geo;
            Renderer.ChildConnectorGeo = to.Geo;
        }
        
        protected virtual ConnectionRenderer _CreateRenderer(bool isVertical)
        {
            return new ConnectionRenderer(isVertical);
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

                onPosChanged?.Invoke();
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
                onMidYChanged?.Invoke();
            }
        }
        public event Action onPosChanged;
        public event Action onMidYChanged;
    }

    public delegate Connection ConnectionCreateDelegate(Connector from, Connector to);

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

        public enum PosType
        {
            PARENT,
            CHILDREN,
            CONDITION,
            INPUT,
            OUTPUT,
        }
        public static readonly string IdentifierChildren = "children";
        public static readonly string IdentifierParent = "parent";
        public static readonly string IdentifierCondition = "cond";

        public static readonly string IdentifierDefault = "default";
        public static readonly string IdentifierIncrement = "inc";
        public static readonly string IdentifierInit = "init";
        public static readonly string IdentifierCond = "cond";

        protected string m_Identifier;
        public string Identifier { get { return m_Identifier; } }

        protected PosType m_PosType;
        public PosType GetPosType { get { return m_PosType; } }

        protected Dir m_Dir = Dir.OUT;
        public Dir GetDir => m_Dir;

        public bool IsVertical { get; private set; }

        protected List<Connection> m_Conns = new List<Connection>();
        public List<Connection> Conns { get { return m_Conns; } }

        protected NodeBase m_Owner;
        public NodeBase Owner { get { return m_Owner; } }

        public ConnectorGeometry Geo { get { return m_Geo; } }
        ConnectorGeometry m_Geo;

        public ConnectionCreateDelegate ConnectionCreator { get; set; }

        int m_AtBottom = -1;  //> Connections on the top of the nodes = -1;
        public Connector(NodeBase node, string identifier, PosType type)
        {
            if (node == null)
                return;
            m_Owner = node;
            m_Identifier = identifier;
            m_PosType = type;

            m_Geo = new ConnectorGeometry()
            {
                Owner = this,
            };

            if (type == PosType.PARENT || type == PosType.INPUT)
            {
                m_Geo.onPosChanged += _OnParentConnectorChanged;
                m_Dir = Dir.IN;
            }
            else
            {
                m_Geo.onPosChanged += _OnChildConnectorChanged;
                m_Dir = Dir.OUT;
            }

            if (type == PosType.CHILDREN)
            {
                m_AtBottom = 1;
            }
            if (type == PosType.INPUT || type == PosType.OUTPUT)
            {
                IsVertical = false;
            }
        }

        public void RecalcMidY()
        {
            if (!IsVertical)
                return;

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

        private void _OnParentConnectorChanged()
        {
            foreach(var conn in m_Conns)
                conn.Ctr.From._OnChildConnectorChanged();
        }


        private void _OnChildConnectorChanged()
        {
            RecalcMidY();
        }

        public double CalcHorizontalPos()
        {
            double miny = double.MaxValue;
            foreach (Connection conn in m_Conns)
            {
                double top = conn.Ctr.To.Geo.Pos.Y;
                miny = Math.Min(miny, top);
            }

            return miny;
        }

        protected virtual bool _CanAdd() { return true; }
        public Connection Connect(Connector target)
        {
            if (target == null)
                return null;

            if (!_CanAdd())
                return null;
            if (!target._CanAdd())
                return null;

            foreach (var conn in target.Conns)
            {
                if (conn.Ctr.From == this)
                {
                    ///> Already connected.
                    return conn;
                }
            }

            Connection connection = ConnectionCreator == null ? new Connection(this, target) : ConnectionCreator(this, target);
            m_Conns.Add(connection);
            target.Conns.Add(connection);

            m_Owner.Conns.MarkDirty();


            RecalcMidY();

            target.Owner.OnConnectFromChanged();
            Owner.OnConnectToChanged();

            ///_CreateConnRenderer(target);
            return connection;
        }

        public static Connection TryConnect(Connector left, Connector right, out Connector parent, out Connector child)
        {
            parent = null;
            child = null;

            if (left == null || right == null)
                return null;

            ///>From same owner. For now we dont support this.
            if (left.m_Owner == right.m_Owner)
                return null;

            if (left.GetDir == Dir.OUT)
                parent = left;
            else
                child = left;

            if (parent != null)
                child = right;
            else
                parent = right;

            if (parent.GetDir == Dir.IN || child.GetDir == Dir.OUT)
                return null;

            return parent.Connect(child);
        }

        public static bool TryDisconnect(Connection.FromTo fromto)
        {
            Connector parent = fromto.From;
            Connector child = fromto.To;

            parent.SimpleRemove(fromto);
            child.SimpleRemove(fromto);


            parent.Owner.Conns.MarkDirty();
            parent.RecalcMidY();

            child.Owner.OnConnectFromChanged();
            parent.Owner.OnConnectToChanged();

            return true;
        }

        public void SimpleRemove(Connection.FromTo fromto)
        {
            for(int i = 0; i < m_Conns.Count; ++i)
            {
                if (m_Conns[i].Ctr.Equals(fromto))
                {
                    m_Conns.RemoveAt(i);
                    return;
                }
            }
        }

        public Connection FindConnection(Connection.FromTo fromto)
        {
            foreach (var c in m_Conns)
            {
                if (c.Ctr.Equals(fromto))
                    return c;
            }
            return null;
        }

        public ConnectionRenderer GetRenderer(Connection.FromTo fromto)
        {
            foreach (var c in m_Conns)
            {
                if (c.Ctr.Equals(fromto))
                {
                    return c.Renderer;
                }
            }
            return null;
        }
    }


    public class Connections : System.Collections.IEnumerable
    {
        NodeBase m_Owner;
        List<Connector> m_Connectors = new List<Connector>();
        public System.Collections.IEnumerable ConnectorsList { get { return m_Connectors; } }

        Connector m_ParentConnector;
        public Connector ParentConnector { get { return m_ParentConnector; } }

        List<Connector> m_InputConnectors = new List<Connector>();
        public System.Collections.IEnumerable InputConnectors { get { return m_InputConnectors; } }


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

        public Connector Add(string identifier, bool isMultiple, Connector.PosType type)
        {
            Connector res;
            if (type == Connector.PosType.PARENT)
            {
                if (m_ParentConnector == null)
                {
                    if (isMultiple)
                        res = new ConnectorMultiple(m_Owner, identifier, type);
                    else
                        res = new ConnectorSingle(m_Owner, identifier, type);
                    m_ParentConnector = res;
                }
                else
                    return null;
            }
            else if (type == Connector.PosType.INPUT)
            {
                foreach (var c in m_InputConnectors)
                {
                    if (c.Identifier == identifier)
                        return null;
                }
                if (isMultiple)
                    res = new ConnectorMultiple(m_Owner, identifier, type);
                else
                    res = new ConnectorSingle(m_Owner, identifier, type);
                m_InputConnectors.Add(res);
            }
            else
            {
                foreach (var c in m_Connectors)
                {
                    if (c.Identifier == identifier && c.GetPosType == type)
                        return null;
                }
                if (isMultiple)
                    res = new ConnectorMultiple(m_Owner, identifier, type);
                else
                    res = new ConnectorSingle(m_Owner, identifier, type);
                m_Connectors.Add(res);
            }
            return res;
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
                        m_Nodes.Add(conn.Ctr.To.Owner);
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

        public Connection Connect(NodeBase target, string identifier)
        {
            if (identifier == null)
                identifier = Connector.IdentifierChildren;
            Connector conn = GetConnector(identifier);
            if (conn == null)
                return null;
            return conn.Connect(target.Conns.ParentConnector);
        }

        public Connection Connect(Connector target, string identifier)
        {
            if (identifier == null)
                identifier = Connector.IdentifierChildren;
            Connector conn = GetConnector(identifier);
            if (conn == null)
                return null;
            return conn.Connect(target);
        }

        public void Sort(Comparison<Connection> comparer)
        {
            foreach (var c in m_Connectors)
            {
                c.Conns.Sort(comparer);
            }
            MarkDirty();
        }

        public static int SortByPosX(Connection aa, Connection bb)
        {
            return aa.Ctr.To.Owner.Geo.Pos.X.CompareTo(bb.Ctr.To.Owner.Geo.Pos.X);
        }
    }

    ///////////////////////////////


    public class ConnectorSingle : Connector
    {
        public ConnectorSingle(NodeBase node, string identifier, PosType type)
            : base(node, identifier, type)
        {
        }

        protected override bool _CanAdd()
        {
            return m_Conns.Count == 0;
        }
    }

    public class ConnectorMultiple : Connector
    {
        public ConnectorMultiple(NodeBase node, string identifier, PosType type)
            : base(node, identifier, type)
        {
        }
    }
}
