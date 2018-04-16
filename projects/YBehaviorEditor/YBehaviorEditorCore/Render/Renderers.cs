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
    public class Renderer : NodeBase
    {
        Node m_Owner;

        Panel m_Panel;
        UINode m_uiFrame;
        public UINode Frame { get { return m_uiFrame; } }

        public Renderer(Node node)
        {
            m_Owner = node;
            m_uiFrame = new UINode
            {
                Node = node
            };
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
            _Refresh();
        }

        private void _Refresh()
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

        public void Render(Panel panel)
        {
            _Render(panel);
        }

        private void _Render(Panel panel)
        {
            m_Panel = panel;
            _RenderSelf();
            _RenderChildren();
        }

        protected void _ClearConns()
        {
            foreach (var pair in m_uiConns)
            {
                m_Panel.Children.Remove(pair.Value);
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
        protected void _RenderChildren()
        {
            _ClearConns();
            foreach (ConnectionHolder conn in m_Owner.Conns.ConnectionsList)
            {
                double y = _CalcHorizontalPos(conn.Conn);
                foreach (Node child in conn.Conn)
                {
                    child.Renderer.Render(m_Panel);
                    //_RenderConn(child, y);
                }
            }
        }

        public void RenderConnections()
        {
            _ClearConns();
            foreach (ConnectionHolder conn in m_Owner.Conns.ConnectionsList)
            {
                double y = _CalcHorizontalPos(conn.Conn);
                foreach (Node child in conn.Conn)
                {
                    child.Renderer.RenderConnections();
                    _RenderConn(child, y);
                }
            }
        }

        protected void _RerenderConn()
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
        protected void _RenderConn(Node child, double horizontalPos)
        {
            UIConnection path = new UIConnection();
            path.SetCanvas(m_Panel);

            _DrawConnLine(path, child, horizontalPos);

            m_Panel.Children.Add(path);
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

            path.ChildHolder = child.Conns.ParentHolder;

            //Point parentPoint = uiConn.TransformToAncestor(m_Panel).Transform(new Point(uiConn.ActualWidth / 2, uiConn.ActualHeight / 2));
            //Point childPoint = childConn.TransformToAncestor(m_Panel).Transform(new Point(childConn.ActualWidth / 2, childConn.ActualHeight / 2));
            Point parentPoint = uiConn.GetPos(m_Panel);
            Point childPoint = childConn.GetPos(m_Panel);

            path.SetWithMidY(parentPoint, childPoint, horizontalPos);
        }

        protected virtual void _RenderSelf()
        {
            _DrawFrame(m_Owner);
            //_DrawName();
            _DrawSelfConnectors();
        }

        private void _DrawFrame(Node node)
        {
            m_uiFrame.SetCanvas(m_Panel);
            m_Panel.Children.Add(m_uiFrame);

            _RefreshSelf();
            m_uiFrame.DataContext = node;
        }

        private void _DrawName()
        {
            //m_uiFrame.name.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding("NickName") { Source = m_Owner });
        }

        private void _DrawSelfConnectors()
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
                uiConnector.SetCanvas(m_Panel);

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
                uiConnector.SetCanvas(m_Panel);

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
                parent.Renderer._RerenderConn();
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

            m_Owner.Renderer._RerenderConn();
        }
    }
}
