using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Xml;

namespace YBehavior.Editor.Core.New
{
    #region Geometry
    /// <summary>
    /// Describe the position and size of a node
    /// </summary>
    public class Geometry
    {
        Rect m_Rect;
        /// <summary>
        /// Locations of four corners
        /// </summary>
        public Rect Rec { get { return m_Rect; } set { m_Rect = value; } }
        //public Thickness Thick { get { return new Thickness(m_Rect.Left, m_Rect.Top, m_Rect.Right, m_Rect.Bottom); } }

        public Geometry()
        {
            m_Rect = new Rect(0, 0, 80, 60);
        }

        //public Point CenterPoint
        //{
        //    get
        //    {
        //        return new Point((m_Rect.Left + m_Rect.Right) / 2, (m_Rect.Top + m_Rect.Bottom) / 2);
        //    }
        //}
        //public Point TopPoint
        //{
        //    get
        //    {
        //        return new Point((m_Rect.Left + m_Rect.Right) / 2, m_Rect.Top);
        //    }
        //}
        //public Point BottomPoint
        //{
        //    get
        //    {
        //        return new Point((m_Rect.Left + m_Rect.Right) / 2, m_Rect.Bottom);
        //    }
        //}

        /// <summary>
        /// Position of the node
        /// </summary>
        public Point Pos
        {
            get { return m_Rect.Location; }
            set { m_Rect.Location = value; }
        }

        /// <summary>
        /// Bottom right pos
        /// </summary>
        public Point BottomRightPos
        {
            get { return m_Rect.BottomRight; }
            set
            {
                Point p = value;
                if (p.X < m_Rect.X)
                    p.X = m_Rect.X;
                if (p.Y < m_Rect.Y)
                    p.Y = m_Rect.Y;
                m_Rect.Width = p.X - m_Rect.X;
                m_Rect.Height = p.Y - m_Rect.Y;
            }
        }

        /// <summary>
        /// Top left pos
        /// </summary>
        public Point TopLeftPos
        {
            get { return m_Rect.TopLeft; }
            set
            {
                Point p = value;
                if (p.Y > m_Rect.Y + m_Rect.Height)
                    p.Y = m_Rect.Y + m_Rect.Height;
                if (p.X > m_Rect.X + m_Rect.Width)
                    p.X = m_Rect.X + m_Rect.Width;
                m_Rect.Height = m_Rect.Y + m_Rect.Height - p.Y;
                m_Rect.Y = p.Y;
                m_Rect.Width = m_Rect.X + m_Rect.Width - p.X;
                m_Rect.X = p.X;
            }
        }

        //public Point TopRightPos
        //{
        //    get { return m_Rect.TopRight; }
        //    set
        //    {
        //        Point p = value;
        //        if (p.X < m_Rect.X)
        //            p.X = m_Rect.X;
        //        if (p.Y > m_Rect.Y + m_Rect.Height)
        //            p.Y = m_Rect.Y + m_Rect.Height;
        //        m_Rect.Height = m_Rect.Y + m_Rect.Height - p.Y;
        //        m_Rect.Y = p.Y;
        //        m_Rect.Width = p.X - m_Rect.X;
        //    }
        //}

        //public Point BottomLeftPos
        //{
        //    get { return m_Rect.BottomLeft; }
        //    set
        //    {
        //        Point p = value;
        //        if (p.Y < m_Rect.Y)
        //            p.Y = m_Rect.Y;
        //        if (p.X > m_Rect.X + m_Rect.Width)
        //            p.X = m_Rect.X + m_Rect.Width;
        //        m_Rect.Width = m_Rect.X + m_Rect.Width - p.X;
        //        m_Rect.X = p.X;
        //        m_Rect.Height = p.Y - m_Rect.Y;
        //    }
        //}

        /// <summary>
        /// Copy from other
        /// </summary>
        /// <param name="other"></param>
        public void Copy(Geometry other)
        {
            m_Rect = other.m_Rect;
        }
    }
    #endregion

    /// <summary>
    /// A wrapper to the node
    /// </summary>
    public class NodeWrapper
    {
        /// <summary>
        /// The node
        /// </summary>
        public NodeBase Node { get; set; }
        /// <summary>
        /// The graph node belongs to
        /// </summary>
        public Graph Graph { get; set; }
    }
    /// <summary>
    /// Base class of node
    /// </summary>
    public class NodeBase
    {
        private uint m_UID = 0;
        private uint m_GUID = 0;
        protected string m_Name = string.Empty;
        protected string m_NickName = string.Empty;
        protected string m_Comment = string.Empty;// "This is a node comment test.";
        protected int m_DisableCount = 0;
        protected NodeDescription m_NodeDescripion;
        /// <summary>
        /// Debug point information
        /// </summary>
        public DebugPointInfo DebugPointInfo { get; } = new DebugPointInfo();
        /// <summary>
        /// Node name
        /// </summary>
        public virtual string Name { get { return m_Name; } }
        /// <summary>
        /// For searching
        /// </summary>
        public virtual IEnumerable<string> TextForFilter { get { return new TextForFilterGetter<BaseTextForFilter>(this); } }
        /// <summary>
        /// Nickname of the node 
        /// </summary>
        public string NickName
        {
            get { return m_NickName; }
            set
            {
                m_NickName = value;
                PropertyChange(RenderProperty.NickName);
            }
        }
        /// <summary>
        /// Comment of the node
        /// </summary>
        public string Comment
        {
            get { return m_Comment; }
            set
            {
                m_Comment = value;
                PropertyChange(RenderProperty.Comment);
            }
        }
        /// <summary>
        /// UID of the node, which is continuous but may change
        /// </summary>
        public uint UID
        {
            get { return m_UID; }
            set
            {
                m_UID = value;
                PropertyChange(RenderProperty.UID);
            }
        }
        /// <summary>
        /// GUID of the node, which may be not continuous but wouldn't change
        /// </summary>
        public uint GUID
        {
            get { return m_GUID; }
            set
            {
                m_GUID = value;
                //PropertyChange(RenderProperty.UID);
            }
        }
        /// <summary>
        /// If this node itself is disabled
        /// </summary>
        public virtual bool SelfDisabled { get { return m_DisableCount > 0; } }
        /// <summary>
        /// If this node itself or its parent is disabled
        /// </summary>
        public virtual bool Disabled
        {
            get { return m_DisableCount > 0; }
            set
            {
                int newValue = value ? 1 : 0;
                if (m_DisableCount == newValue)
                    return;

                m_DisableCount = newValue;
                PropertyChange(RenderProperty.Disabled);

                Graph.RefreshNodeUID(0);
            }
        }
        /// <summary>
        /// Note of the node which will keep displayed in node ui
        /// </summary>
        public virtual string Note => string.Empty;
        /// <summary>
        /// Icon of the node
        /// </summary>
        public virtual string Icon => Connector.IdentifierParent;
        /// <summary>
        /// Description of the node which will be shown in tips
        /// </summary>
        public string Description => m_NodeDescripion == null ? string.Empty : m_NodeDescripion.node;
        protected NodeWrapper m_Wrapper;
        /// <summary>
        /// The tree/fsm this node belongs to
        /// </summary>
        public Graph Graph
        {
            get { return m_Wrapper.Graph; }
            set { m_Wrapper.Graph = value; }
        }
        /// <summary>
        /// Position and size
        /// </summary>
        public Geometry Geo { get; } = new Geometry();

        protected NodeBaseRenderer m_Renderer;
        /// <summary>
        /// ViewModel
        /// </summary>
        public NodeBaseRenderer Renderer { get { return m_Renderer; } }
        /// <summary>
        /// Get or Create ViewModel
        /// </summary>
        public NodeBaseRenderer ForceGetRenderer
        {
            get
            {
                if (m_Renderer == null)
                    _CreateRenderer();
                return m_Renderer;
            }
        }
        protected virtual void _CreateRenderer()
        {
            m_Renderer = new NodeBaseRenderer(this);
        }
        protected Connections m_Connections;
        /// <summary>
        /// Connectors of the node
        /// </summary>
        public Connections Conns { get { return m_Connections; } }

        public NodeBase()
        {
            m_Connections = new Connections(this);
        }
        /// <summary>
        /// Set debug point type
        /// </summary>
        /// <param name="count">DebugPointInfo.HitCount</param>
        public void SetDebugPoint(int count)
        {
            DebugPointInfo.HitCount = count;
            PropertyChange(RenderProperty.DebugPoint);
            DebugMgr.Instance.SetDebugPoint(UID, count);
        }

        protected virtual void _CreateWrapper()
        {
            m_Wrapper = new NodeWrapper();
        }
        /// <summary>
        /// Create basic data
        /// </summary>
        public virtual void CreateBase()
        {
            _CreateWrapper();
            m_Wrapper.Node = this;
        }

        /// <summary>
        /// Parent changed
        /// </summary>
        public virtual void OnConnectFromChanged()
        {

        }

        /// <summary>
        /// Child changed
        /// </summary>
        public virtual void OnConnectToChanged()
        {

        }
        /// <summary>
        /// Added to tree/fsm
        /// </summary>
        public virtual void OnAddToGraph()
        {

        }
        /// <summary>
        /// Check if this node can be connected to other
        /// </summary>
        /// <param name="myCtr">Connector in this node</param>
        /// <param name="otherCtr">Connector in other node</param>
        /// <returns></returns>
        public virtual bool CanConnect(Connector myCtr, Connector otherCtr) => true;

        public void PropertyChange(RenderProperty property)
        {
            if (m_Renderer != null)
                m_Renderer.PropertyChange(property);
        }
        /// <summary>
        /// Clone the node
        /// </summary>
        /// <returns></returns>
        public virtual NodeBase Clone()
        {
            NodeBase other = Activator.CreateInstance(this.GetType()) as NodeBase;
            other.CreateBase();
            other.m_Name = this.m_Name;
            other.m_NickName = this.m_NickName;
            other.Geo.Copy(this.Geo);
            other.Geo.Pos = other.Geo.Pos;
            other.m_NodeDescripion = this.m_NodeDescripion;

            return other;
        }
        /// <summary>
        /// Check if the node is valid
        /// </summary>
        /// <returns></returns>
        public virtual bool CheckValid() { return true; }
        /// <summary>
        /// Move the node
        /// </summary>
        /// <param name="delta"></param>
        /// <param name="param">if 0, children will be moved together</param>
        public void Move(Vector delta, int param)
        {
            Geo.Pos = Geo.Pos + delta;

            if (Renderer != null)
                Renderer.OnMove();

            if (param == 0)
            {
                foreach (NodeBase child in Conns)
                {
                    child.Move(delta, param);
                }
            }

        }
        /// <summary>
        /// Called when a node is just created or added
        /// </summary>
        /// <param name="node"></param>
        /// <param name="graph"></param>
        public static void OnAddToGraph(NodeBase node, Graph graph)
        {
            node.Graph = graph;
            node.OnAddToGraph();
        }
    }
}
