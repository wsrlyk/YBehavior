using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace YBehavior.Editor.Core
{
    public class RenderCanvas
    {
        public FrameworkElement Panel { get; set; }
    }

    public class ConnectorGeometry
    {
        public delegate void OnPosChanged();
        public double X
        {
            get { return m_Pos.X; }
            set
            {
                m_Pos.X = value;
                if (onPosChanged != null)
                    onPosChanged();
            }
        }
        public double Y
        {
            get { return m_Pos.Y; }
            set
            {
                m_Pos.Y = value;
                if (onPosChanged != null)
                    onPosChanged();
            }
        }

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

        public OnPosChanged onPosChanged;
    }

    public class ConnectionRenderer : System.ComponentModel.INotifyPropertyChanged
    {
        public Point ParentPos { get { return ParentConnectorGeo.Pos; } }
        public Point ChildPos { get { return ChildConnectorGeo.Pos; } }

        ConnectorGeometry m_ParentConnectorGeo;
        public ConnectorGeometry ParentConnectorGeo
        {
            get { return m_ParentConnectorGeo; }
            set { m_ParentConnectorGeo = value; m_ParentConnectorGeo.onPosChanged += _OnParentPosChanged; }
        }
        ConnectorGeometry m_ChildConnectorGeo;
        public ConnectorGeometry ChildConnectorGeo
        {
            get { return m_ChildConnectorGeo; }
            set { m_ChildConnectorGeo = value; m_ChildConnectorGeo.onPosChanged += _OnChildPosChanged; }
        }

        public Renderer ParentRenderer { get; set; }
        public Renderer ChildRenderer { get; set; }

        void _OnChildPosChanged()
        {
            OnPropertyChanged("ChildPos");
        }

        void _OnParentPosChanged()
        {
            OnPropertyChanged("ParentPos");
        }

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        internal protected void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
            }
        }
    }

    public class Renderer : System.ComponentModel.INotifyPropertyChanged
    {
        Node m_Owner;
        public Node Owner { get { return m_Owner; } }

        RenderCanvas m_Canvas = new RenderCanvas();
        public RenderCanvas RenderCanvas { get { return m_Canvas; } }

        //UINode m_uiFrame;
        //public UINode Frame { get { return m_uiFrame; } }

        public Geometry Geo { get; } = new Geometry();
        Dictionary<string, ConnectorGeometry> m_ConnectorGeos = new Dictionary<string, ConnectorGeometry>();
        public ConnectorGeometry GetConnectorGeometry(string identifier)
        {
            if (m_ConnectorGeos.TryGetValue(identifier, out ConnectorGeometry geo))
            {
                return geo;
            }
            return null;
        }

        public double MidY { get; set; }
        Dictionary<Node, ConnectionRenderer> m_Connections = new Dictionary<Node, ConnectionRenderer>();
        public ConnectionRenderer GetConnectionRenderer(Node node)
        {
            if (m_Connections.TryGetValue(node, out ConnectionRenderer renderer))
            {
                return renderer;
            }
            return null;
        }
        public object UINodeRef { get; set; }

        public Renderer(Node node)
        {
            m_Owner = node;
            //m_uiFrame = new UINode
            //{
            //    Node = node
            //};

            _CreateSelf();
        }

        //Dictionary<Node, UIConnection> m_uiConns = new Dictionary<Node, UIConnection>();

        //Dictionary<string, UIConnector> m_uiConnectors = new Dictionary<string, UIConnector>();
        //public UIConnector GetConnector(string identifier)
        //{
        //    UIConnector conn;
        //    m_uiConnectors.TryGetValue(identifier, out conn);
        //    return conn;
        //}

        public void Refresh()
        {
            _RefreshSelf();

            foreach (Node child in m_Owner.Conns)
            {
                child.Renderer.Refresh();
            }
        }

        private void _RefreshSelf()
        {
            // TODO
            //m_uiFrame.SetDebug(NodeState.NS_INVALID);

            //Canvas.SetLeft(m_uiFrame, Geo.Pos.X);
            //Canvas.SetTop(m_uiFrame, Geo.Pos.Y);
        }

        public void RefreshDebug(bool bInstant)
        {
            // TODO
            //NodeState state = DebugMgr.Instance.IsDebugging() ? DebugMgr.Instance.GetRunState(m_Owner.UID) : NodeState.NS_INVALID;
            //if (bInstant)
            //    m_uiFrame.SetDebugInstant(state);
            //else
            //    m_uiFrame.SetDebug(state);

            //foreach (Node child in m_Owner.Conns)
            //{
            //    child.Renderer.RefreshDebug(bInstant);
            //}
        }

        public void AddedToPanel(FrameworkElement panel)
        {
            RenderMgr.Instance.AddNode(this);
            foreach (Node child in m_Owner.Conns)
            {
                child.Renderer.AddedToPanel(panel);
            }

            m_Canvas.Panel = panel;

            //panel.Children.Add(m_uiFrame);
            //_RefreshSelf();
            //foreach (Node child in m_Owner.Conns)
            //{
            //    child.Renderer.AddedToPanel(panel);
            //}

            //foreach (UIConnection uiconn in m_uiConns.Values)
            //{
            //    panel.Children.Add(uiconn);
            //}

            //panel.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Render, new Action(RefreshConn));
        }

        protected void _ClearConns()
        {
            //foreach (var pair in m_uiConns)
            //{
            //    m_Canvas.Panel.Children.Remove(pair.Value);
            //}
            //m_uiConns.Clear();
            m_Connections.Clear();
        }
        protected double _CalcHorizontalPos(Connection conn)
        {
            double y = 0;
            double miny = double.MaxValue;
            foreach (Node child in conn)
            {
                double top = child.Renderer.Geo.Pos.Y;
                y += top;
                miny = Math.Min(miny, top);
            }
            y /= conn.NodeCount;
            y -= 10;
            miny -= 10;
            y = Math.Min(miny, y);
            y = Math.Max(y, Geo.Pos.Y + /*Frame.ActualHeight + */10);
            return y;
        }

        public void CreateConnections()
        {
            _ClearConns();
            foreach (ConnectionHolder conn in m_Owner.Conns.ConnectionsList)
            {
                double y = _CalcHorizontalPos(conn.Conn);
                foreach (Node child in conn.Conn)
                {
                    child.Renderer.CreateConnections();
                    _CreateConn(child, y);
                }
            }

            //if (m_Canvas.Panel != null)
            //    m_Canvas.Panel.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Render, new Action(RefreshConn));
        }

        public void RefreshConns()
        {
            RefreshConn();
            foreach (Node child in m_Owner.Conns)
            {
                child.Renderer.RefreshConns();
            }
        }

        public void RefreshConn()
        {
            foreach (ConnectionHolder conn in m_Owner.Conns.ConnectionsList)
            {
                double y = _CalcHorizontalPos(conn.Conn);
                foreach (Node child in conn.Conn)
                {
                    ConnectionRenderer connectionRenderer;
                    if (!m_Connections.TryGetValue(child, out connectionRenderer))
                        continue;

                    _SetConn(connectionRenderer, child, y);
                }
            }
        }
        protected void _CreateConn(Node child, double horizontalPos)
        {
            //UIConnection path = new UIConnection();
            //path.SetCanvas(m_Canvas);
            //path.ChildHolder = child.Conns.ParentHolder;

            //if (m_Canvas.Panel != null)
            //    m_Canvas.Panel.Children.Add(path);
            //Panel.SetZIndex(path, -999);
            //m_uiConns.Add(child, path);
            ConnectionRenderer connectionRenderer = new ConnectionRenderer();
            _SetConn(connectionRenderer, child, horizontalPos);
            m_Connections.Add(child, connectionRenderer);
            RenderMgr.Instance.AddConnection(connectionRenderer);
        }

        protected void _SetConn(ConnectionRenderer connectionRenderer, Node child, double horizontalPos)
        {
            connectionRenderer.ParentConnectorGeo = this.GetConnectorGeometry(child.ParentConn.Identifier);
            connectionRenderer.ChildConnectorGeo = child.Renderer.GetConnectorGeometry(Connection.IdentifierParent);
            connectionRenderer.ParentRenderer = this;
            connectionRenderer.ChildRenderer = child.Renderer;
        }

        //protected void _DrawConnLine(UIConnection path, Node child, double horizontalPos)
        //{
        //    if (child.ParentConn == null)
        //        return;
            //UIConnector uiConn = GetConnector(child.ParentConn.Identifier);
            //if (uiConn == null)
            //    return;
            //UIConnector childConn = child.Renderer.GetConnector(Connection.IdentifierParent);
            //if (childConn == null)
            //    return;

            //Point parentPoint = uiConn.GetPos(m_Canvas.Panel);
            //Point childPoint = childConn.GetPos(m_Canvas.Panel);

            //path.SetWithMidY(parentPoint, childPoint, horizontalPos);
        //}

        protected virtual void _CreateSelf()
        {
            //_CreateFrame(m_Owner);
            _CreateConnectors();
            _SetCommentPos();
        }

        //private void _CreateFrame(Node node)
        //{
        //    m_uiFrame.SetCanvas(m_Canvas);
        //    m_uiFrame.DataContext = node;
        //}

        private void _CreateConnectors()
        {
            m_ConnectorGeos.Clear();

            if (m_Owner.Conns.ParentHolder != null)
            {
                ConnectorGeometry connector = new ConnectorGeometry();
                m_ConnectorGeos.Add(Connection.IdentifierParent, connector);
            }

            foreach (ConnectionHolder conn in m_Owner.Conns.ConnectionsList)
            {
                if (conn.Conn is ConnectionNone)
                    continue;

                ConnectorGeometry connector = new ConnectorGeometry();
                m_ConnectorGeos.Add(conn.Conn.Identifier, connector);
            }
        }

        private void _SetCommentPos()
        {
            //if (m_uiFrame.bottomConnectors.Children.Count > 0)
            //    DockPanel.SetDock(m_uiFrame.commentBorder, Dock.Right);
            //else
            //    DockPanel.SetDock(m_uiFrame.commentBorder, Dock.Bottom);
        }
        public void DragMain(Vector delta)
        {
            _Move(delta);
            _OnPosChanged();
        }

        public void SetPos(Point pos)
        {
            _Move(pos - Geo.Pos);
            _OnPosChanged();
        }

        void _OnPosChanged()
        {
            Node parent = m_Owner.Parent as Node;
            if (parent != null)
            {
                parent.Renderer.RefreshConn();
                parent.OnChildPosChanged();
            }
        }
        void _Move(Vector delta)
        {
            Geo.Pos = Geo.Pos + delta;

            OnPropertyChanged("Geo");
            // TODO
            //Canvas.SetLeft(m_uiFrame, Geo.Pos.X);
            //Canvas.SetTop(m_uiFrame, Geo.Pos.Y);

            foreach (Node child in m_Owner.Conns)
            {
                child.Renderer._Move(delta);
            }

            RefreshConn();
        }

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        internal protected void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
