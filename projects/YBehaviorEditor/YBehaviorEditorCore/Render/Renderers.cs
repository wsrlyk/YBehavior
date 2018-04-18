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
        public Panel Panel { get; set; }
    }
    public class Renderer : NodeBase
    {
        Node m_Owner;

        RenderCanvas m_Canvas = new RenderCanvas();

        UINode m_uiFrame;
        public UINode Frame { get { return m_uiFrame; } }

        public Renderer(Node node)
        {
            m_Owner = node;
            m_uiFrame = new UINode
            {
                Node = node
            };

            _CreateSelf();
        }

        Dictionary<Node, UIConnection> m_uiConns = new Dictionary<Node, UIConnection>();

        Dictionary<string, UIConnector> m_uiConnectors = new Dictionary<string, UIConnector>();
        public UIConnector GetConnector(string identifier)
        {
            UIConnector conn;
            m_uiConnectors.TryGetValue(identifier, out conn);
            return conn;
        }

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
            m_uiFrame.SetDebug(DebugMgr.Instance.IsDebugging() ? DebugMgr.Instance.GetRunState(m_Owner.UID) : NodeState.NS_INVALID);
            Canvas.SetLeft(m_uiFrame, m_Owner.Geo.Pos.X);
            Canvas.SetTop(m_uiFrame, m_Owner.Geo.Pos.Y);
        }

        public void AddedToPanel(Panel panel)
        {
            m_Canvas.Panel = panel;

            panel.Children.Add(m_uiFrame);
            _RefreshSelf();
            foreach (Node child in m_Owner.Conns)
            {
                child.Renderer.AddedToPanel(panel);
            }

            foreach (UIConnection uiconn in m_uiConns.Values)
            {
                panel.Children.Add(uiconn);
            }

            panel.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Render, new Action(RefreshConn));
        }

        protected void _ClearConns()
        {
            foreach (var pair in m_uiConns)
            {
                m_Canvas.Panel.Children.Remove(pair.Value);
            }
            m_uiConns.Clear();
        }
        protected double _CalcHorizontalPos(Connection conn)
        {
            double y = 0;
            double miny = double.MaxValue;
            foreach (Node child in conn)
            {
                double top = Canvas.GetTop(child.Renderer.Frame);
                y += top;
                miny = Math.Min(miny, top);
            }
            y /= conn.NodeCount;
            y -= 10;
            miny -= 10;
            y = Math.Min(miny, y);
            y = Math.Max(y, Canvas.GetTop(Frame) + Frame.ActualHeight + 10);
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

            if (m_Canvas.Panel != null)
                m_Canvas.Panel.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Render, new Action(RefreshConn));
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
                    UIConnection path;
                    if (!m_uiConns.TryGetValue(child, out path))
                        continue;

                    _DrawConnLine(path, child, y);
                }
            }
        }
        protected void _CreateConn(Node child, double horizontalPos)
        {
            UIConnection path = new UIConnection();
            path.SetCanvas(m_Canvas);
            path.ChildHolder = child.Conns.ParentHolder;
            //_DrawConnLine(path, child, horizontalPos);

            if (m_Canvas.Panel != null)
                m_Canvas.Panel.Children.Add(path);
            Panel.SetZIndex(path, -999);
            m_uiConns.Add(child, path);
        }

        protected void _DrawConnLine(UIConnection path, Node child, double horizontalPos)
        {
            if (child.ParentConn == null)
                return;
            UIConnector uiConn = GetConnector(child.ParentConn.Identifier);
            if (uiConn == null)
                return;
            UIConnector childConn = child.Renderer.GetConnector(Connection.IdentifierParent);
            if (childConn == null)
                return;

            Point parentPoint = uiConn.GetPos(m_Canvas.Panel);
            Point childPoint = childConn.GetPos(m_Canvas.Panel);

            path.SetWithMidY(parentPoint, childPoint, horizontalPos);
        }

        protected virtual void _CreateSelf()
        {
            _CreateFrame(m_Owner);
            _CreateConnectors();
        }

        private void _CreateFrame(Node node)
        {
            m_uiFrame.SetCanvas(m_Canvas);
            m_uiFrame.DataContext = node;
        }

        private void _CreateConnectors()
        {
            m_uiConnectors.Clear();
            m_uiFrame.topConnectors.Children.Clear();
            m_uiFrame.bottomConnectors.Children.Clear();

            if (m_Owner.Conns.ParentHolder != null)
            {
                UIConnector uiConnector = new UIConnector
                {
                    Title = Connection.IdentifierParent,
                    ConnHolder = m_Owner.Conns.ParentHolder
                };
                uiConnector.SetCanvas(m_Canvas);

                m_uiFrame.topConnectors.Children.Add(uiConnector);

                m_uiConnectors.Add(Connection.IdentifierParent, uiConnector);
            }

            foreach (ConnectionHolder conn in m_Owner.Conns.ConnectionsList)
            {
                if (conn.Conn is ConnectionNone)
                    continue;

                UIConnector uiConnector = new UIConnector
                {
                    Title = conn.Conn.Identifier,
                    ConnHolder = conn
                };
                uiConnector.SetCanvas(m_Canvas);

                m_uiFrame.bottomConnectors.Children.Add(uiConnector);

                m_uiConnectors.Add(conn.Conn.Identifier, uiConnector);
            }
        }

        public void DragMain(Vector delta, Point pos)
        {
            _Move(delta);
            Node parent = m_Owner.Parent as Node;
            if (parent != null)
            {
                parent.Renderer.RefreshConn();
                parent.OnChildPosChanged();
            }
        }
        void _Move(Vector delta)
        {
            m_Owner.Geo.Pos = m_Owner.Geo.Pos + delta;
            Canvas.SetLeft(m_uiFrame, m_Owner.Geo.Pos.X);
            Canvas.SetTop(m_uiFrame, m_Owner.Geo.Pos.Y);

            foreach (Node child in m_Owner.Conns)
            {
                child.Renderer._Move(delta);
            }

            RefreshConn();
        }
    }
}
