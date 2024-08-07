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
        /// <summary>
        /// A struct describing head and tail of a line
        /// </summary>
        public struct FromTo : IEquatable<FromTo>
        {
            public Connector From { get; set; }
            public Connector To { get; set; }

            public bool Equals(FromTo other)
            {
                return From == other.From && To == other.To;
            }
        }
        /// <summary>
        /// Head and tail of the line
        /// </summary>
        public FromTo Ctr { get; set; }

        private string m_Note = string.Empty;
        /// <summary>
        /// Description
        /// </summary>
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
        /// <summary>
        /// The ViewModel of the line
        /// </summary>
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

    /// <summary>
    /// Position of the connector
    /// </summary>
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
        /// <summary>
        /// A position above all the children nodes connecting this connector
        /// </summary>
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
            INPUT,
            OUTPUT,
            MAX,
        }
        public static readonly string IdentifierChildren = "children";
        public static readonly string IdentifierParent = "parent";
        public static readonly string IdentifierCondition = "condition";

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

        /// <summary>
        /// If this is a connection to the "parent" connector of a tree node, then it is vertical
        /// </summary>
        public bool IsVertical { get; private set; }

        protected List<Connection> m_Conns = new List<Connection>();
        /// <summary>
        /// All connections from this connector
        /// </summary>
        public List<Connection> Conns { get { return m_Conns; } }

        protected NodeBase m_Owner;
        public NodeBase Owner { get { return m_Owner; } }

        public ConnectorGeometry Geo => m_Geo;
        ConnectorGeometry m_Geo;

        /// <summary>
        /// ViewModel
        /// </summary>
        public ConnectorRenderer Renderer => m_Renderer;
        ConnectorRenderer m_Renderer;
        public ConnectionCreateDelegate ConnectionCreator { get; set; }

        protected bool m_bIsVisible = true;
        public bool IsVisible
        {
            get { return m_bIsVisible; }
            set
            {
                if (m_bIsVisible != value)
                {
                    m_bIsVisible = value;
                    IsVisibleEvent?.Invoke();
                }
            }
        }
        public event Action IsVisibleEvent;

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

            m_Renderer = new ConnectorRenderer(this);

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
            else
            {
                IsVertical = true;
            }
        }

        /// <summary>
        /// Calculate the MidY when children change
        /// </summary>
        public void RecalcMidY()
        {
            if (!IsVertical)
                return;

            double childPos = _CalcVerticalPos();
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

        double _CalcVerticalPos()
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
        /// <summary>
        /// Make a new connection to another connector
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public Connection Connect(Connector target)
        {
            if (target == null)
                return null;

            if (!this.IsVisible || !target.IsVisible)
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

            if (!Owner.CanConnect(this, target))
                return null;

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

        /// <summary>
        /// Check and Try to make connection between two connectors.
        /// Only an incoming connector and an outgoing connector could make a connection.
        /// </summary>
        /// <param name="left">One connector</param>
        /// <param name="right">Another connector</param>
        /// <param name="parent">Outgoing connector</param>
        /// <param name="child">Incoming connector</param>
        /// <returns>If null, it fails to make a connection</returns>
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

        /// <summary>
        /// Disconnect two connectors
        /// </summary>
        /// <param name="fromto"></param>
        /// <returns></returns>
        public static bool TryDisconnect(Connection.FromTo fromto)
        {
            Connector parent = fromto.From;
            Connector child = fromto.To;

            parent._SimpleRemove(fromto);
            child._SimpleRemove(fromto);


            parent.Owner.Conns.MarkDirty();
            parent.RecalcMidY();

            child.Owner.OnConnectFromChanged();
            parent.Owner.OnConnectToChanged();

            return true;
        }

        void _SimpleRemove(Connection.FromTo fromto)
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

        /// <summary>
        /// Try to find the connection of a pair of connectors
        /// </summary>
        /// <param name="fromto"></param>
        /// <returns></returns>
        public Connection FindConnection(Connection.FromTo fromto)
        {
            foreach (var c in m_Conns)
            {
                if (c.Ctr.Equals(fromto))
                    return c;
            }
            return null;
        }

        /// <summary>
        /// Try to find the ViewModel of a pair of connectors
        /// </summary>
        /// <param name="fromto"></param>
        /// <returns></returns>
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

    /// <summary>
    /// The collection of connectors in a node
    /// </summary>
    public class Connections : System.Collections.IEnumerable
    {
        NodeBase m_Owner;
        List<Connector>[] m_ConnectorsList = new List<Connector>[]
        {
            new List<Connector>(),
            new List<Connector>(),
            new List<Connector>(),
            new List<Connector>(),
            new List<Connector>()
        };
        /// <summary>
        /// For searching
        /// </summary>
        public System.Collections.IEnumerable AllConnectors => m_AllConnectorsList;
        List<Connector> m_AllConnectorsList = new List<Connector>();
        /// <summary>
        /// The connections starting from here, including children and output
        /// </summary>
        public System.Collections.IEnumerable MainConnectors => m_MainConnectorsList;
        List<Connector> m_MainConnectorsList = new List<Connector>();
        /// <summary>
        /// The connections of children in tree
        /// </summary>
        public System.Collections.IEnumerable ChildrenConnectors => m_ChildrenConnectorsList;
        List<Connector> m_ChildrenConnectorsList = new List<Connector>();

        /// <summary>
        /// Connect to parent
        /// </summary>
        public Connector ParentConnector { get; private set; }
        /// <summary>
        /// When an input pin don't want to be assigned with a constant value or a variable in the node panel 
        /// but wants to be connected directly with another output pin, an InputConnector would be created here
        /// </summary>
        public System.Collections.IEnumerable InputConnectors => m_InputConnectors;
        List<Connector> m_InputConnectors = new List<Connector>();
        /// <summary>
        /// When an output pin don't want to be assigned with a constant value or a variable in the node panel 
        /// but wants to be connected directly with another input pin, an OutputConnector would be created here
        /// </summary>
        public System.Collections.IEnumerable OutputConnectors => m_OutputConnectors;
        List<Connector> m_OutputConnectors = new List<Connector>();

        /// <summary>
        /// Children nodes 
        /// </summary>
        List<NodeBase> m_Nodes = new List<NodeBase>();
        public int NodeCount { get { _ProcessDirty(); return m_Nodes.Count; } }

        bool m_bDirty = true;
        /// <summary>
        /// Called when children change
        /// </summary>
        public void MarkDirty()
        {
            m_bDirty = true;
        }

        public Connections(NodeBase owner)
        {
            m_Owner = owner;
        }

        /// <summary>
        /// Create a connector
        /// </summary>
        /// <param name="identifier">Name of the connector</param>
        /// <param name="isMultiple">Whether multiple nodes could connect this connector</param>
        /// <param name="type"></param>
        /// <returns></returns>
        public Connector Add(string identifier, bool isMultiple, Connector.PosType type)
        {
            Connector res;
            foreach (Connector c in AllConnectors)
            {
                if (c.Identifier == identifier && c.GetPosType == type)
                    return null;
            }
            if (isMultiple)
                res = new ConnectorMultiple(m_Owner, identifier, type);
            else
                res = new ConnectorSingle(m_Owner, identifier, type);
            if (type == Connector.PosType.PARENT)
            {
                if (ParentConnector == null)
                {
                    ParentConnector = res;
                }
                else
                    return null;
            }
            else if (type == Connector.PosType.INPUT)
            {
                m_InputConnectors.Add(res);
            }
            else if (type == Connector.PosType.OUTPUT)
            {
                m_OutputConnectors.Add(res);
                m_MainConnectorsList.Add(res);
            }
            else
            {
                if (identifier == Connector.IdentifierCondition)
                {
                    m_ChildrenConnectorsList.Insert(0, res);
                    m_MainConnectorsList.Insert(0, res);
                }
                else
                {
                    m_ChildrenConnectorsList.Add(res);
                    m_MainConnectorsList.Add(res);
                }
            }
            m_AllConnectorsList.Add(res);
            return res;
        }

        void _ProcessDirty()
        {
            if (m_bDirty)
            {
                m_bDirty = false;
                m_Nodes.Clear();
                foreach (var ctrs in m_ChildrenConnectorsList)
                {
                    foreach (Connection conn in ctrs.Conns)
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

        /// <summary>
        /// Try to find a connector
        /// </summary>
        /// <param name="identifier">Name</param>
        /// <param name="posType">Type</param>
        /// <returns></returns>
        public Connector GetConnector(string identifier, Connector.PosType posType)
        {
            foreach (var conn in m_AllConnectorsList)
            {
                if (conn.GetPosType == posType && conn.Identifier == identifier)
                    return conn;
            }

            return null;
        }

        /// <summary>
        /// Try to connect the parent connector of a node with a connector
        /// </summary>
        /// <param name="target"></param>
        /// <param name="identifier">Connector name</param>
        /// <returns></returns>
        public Connection Connect(NodeBase target, string identifier)
        {
            Connector conn = GetConnector(identifier, Connector.PosType.CHILDREN);
            if (conn == null)
                return null;
            return conn.Connect(target.Conns.ParentConnector);
        }

        public void Sort(Comparison<Connection> comparer)
        {
            foreach (var c in m_ChildrenConnectorsList)
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

    /// <summary>
    /// Connector that could have only one child
    /// </summary>
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
    /// <summary>
    /// Connector that could have Multiple children
    /// </summary>
    public class ConnectorMultiple : Connector
    {
        public ConnectorMultiple(NodeBase node, string identifier, PosType type)
            : base(node, identifier, type)
        {
        }
    }
}
