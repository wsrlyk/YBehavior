using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Xml;

namespace YBehavior.Editor.Core.New
{
    #region Geometry
    public class Geometry
    {
        Rect m_Rect;
        public Rect Rec { get { return m_Rect; } set { m_Rect = value; } }
        public Thickness Thick { get { return new Thickness(m_Rect.Left, m_Rect.Top, m_Rect.Right, m_Rect.Bottom); } }

        public Geometry()
        {
            m_Rect = new Rect(0, 0, 80, 60);
        }

        public Point CenterPoint
        {
            get
            {
                return new Point((m_Rect.Left + m_Rect.Right) / 2, (m_Rect.Top + m_Rect.Bottom) / 2);
            }
        }
        public Point TopPoint
        {
            get
            {
                return new Point((m_Rect.Left + m_Rect.Right) / 2, m_Rect.Top);
            }
        }
        public Point BottomPoint
        {
            get
            {
                return new Point((m_Rect.Left + m_Rect.Right) / 2, m_Rect.Bottom);
            }
        }

        public Point Pos
        {
            get { return m_Rect.Location; }
            set { m_Rect.Location = value; }
        }

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

        public Point TopRightPos
        {
            get { return m_Rect.TopRight; }
            set
            {
                Point p = value;
                if (p.X < m_Rect.X)
                    p.X = m_Rect.X;
                if (p.Y > m_Rect.Y + m_Rect.Height)
                    p.Y = m_Rect.Y + m_Rect.Height;
                m_Rect.Height = m_Rect.Y + m_Rect.Height - p.Y;
                m_Rect.Y = p.Y;
                m_Rect.Width = p.X - m_Rect.X;
            }
        }

        public Point BottomLeftPos
        {
            get { return m_Rect.BottomLeft; }
            set
            {
                Point p = value;
                if (p.Y < m_Rect.Y)
                    p.Y = m_Rect.Y;
                if (p.X > m_Rect.X + m_Rect.Width)
                    p.X = m_Rect.X + m_Rect.Width;
                m_Rect.Width = m_Rect.X + m_Rect.Width - p.X;
                m_Rect.X = p.X;
                m_Rect.Height = p.Y - m_Rect.Y;
            }
        }
        public void Copy(Geometry other)
        {
            m_Rect = other.m_Rect;
        }
    }
    #endregion

    public class NodeWrapper
    {
        public NodeBase Node { get; set; }
        public Graph Graph { get; set; }
    }


    public class NodeBase
    {
        private uint m_UID = 0;
        protected string m_Name = "";
        protected string m_NickName;
        protected string m_Comment = string.Empty;// "This is a node comment test.";
        protected int m_DisableCount = 0;
        protected NodeDescription m_NodeDescripion;

        public DebugPointInfo DebugPointInfo { get; } = new DebugPointInfo();

        public virtual string Name { get { return m_Name; } }
        public string NickName
        {
            get { return m_NickName; }
            set
            {
                m_NickName = value;
                PropertyChange(RenderProperty.NickName);
            }
        }
        public string Comment
        {
            get { return m_Comment; }
            set
            {
                m_Comment = value;
                PropertyChange(RenderProperty.Comment);
            }
        }
        public uint UID
        {
            get { return m_UID; }
            set
            {
                m_UID = value;
                PropertyChange(RenderProperty.UID);
            }
        }

        public virtual bool SelfDisabled { get { return m_DisableCount > 0; } }
        public virtual bool Disabled
        {
            get { return m_DisableCount > 0; }
            set
            {
                int newValue = value ? 1 : 0;
                if (m_DisableCount == newValue)
                    return;

                m_DisableCount = newValue;
                PropertyChange(RenderProperty.Disable);

                Graph.RefreshNodeUID();
            }
        }

        public virtual string Note => string.Empty;
        public virtual string Icon => Connector.IdentifierParent;
        public string Description => m_NodeDescripion == null ? string.Empty : m_NodeDescripion.node;
        protected NodeWrapper m_Wrapper;
        public Graph Graph
        {
            get { return m_Wrapper.Graph; }
            set { m_Wrapper.Graph = value; }
        }
        public Geometry Geo { get; } = new Geometry();

        protected NodeBaseRenderer m_Renderer;
        public NodeBaseRenderer Renderer { get { return m_Renderer; } }
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
        public Connections Conns { get { return m_Connections; } }

        public NodeBase()
        {
            m_Connections = new Connections(this);
        }

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

        public virtual void OnAddToGraph()
        {

        }

        public void PropertyChange(RenderProperty property)
        {
            if (m_Renderer != null)
                m_Renderer.PropertyChange(property);
        }

        public virtual NodeBase Clone()
        {
            NodeBase other = Activator.CreateInstance(this.GetType()) as NodeBase;
            other.CreateBase();
            other.m_Name = this.m_Name;
            other.m_NickName = this.m_NickName;
            other.Geo.Copy(this.Geo);
            other.Geo.Pos = other.Geo.Pos + new Vector(5, 5);
            other.m_NodeDescripion = this.m_NodeDescripion;

            return other;
        }

        public virtual bool CheckValid() { return true; }

        public static void OnAddToGraph(NodeBase node, object graph)
        {
            node.Graph = graph as Graph;
            node.OnAddToGraph();
        }
    }



}
