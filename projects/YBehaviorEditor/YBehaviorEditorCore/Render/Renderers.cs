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

        public Renderer(Node node)
        {
            m_Owner = node;
        }

        Panel m_Panel;
        UINode m_uiFrame = new UINode();
        public UINode Frame { get { return m_uiFrame; } }

        Dictionary<Node, Path> m_uiConns = new Dictionary<Node, Path>();

        Dictionary<string, UIConnector> m_uiConnectors = new Dictionary<string, UIConnector>();
        public UIConnector GetConnector(string identifier)
        {
            UIConnector conn;
            m_uiConnectors.TryGetValue(identifier, out conn);
            return conn;
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
            foreach (Connection conn in m_Owner.Conns.ConnectionsList)
            {
                double y = _CalcHorizontalPos(conn);
                foreach (Node child in conn)
                {
                    child.Renderer.Render(m_Panel);
                    //_RenderConn(child, y);
                }
            }
        }

        public void RenderConnections()
        {
            _ClearConns();
            foreach (Connection conn in m_Owner.Conns.ConnectionsList)
            {
                double y = _CalcHorizontalPos(conn);
                foreach (Node child in conn)
                {
                    child.Renderer.RenderConnections();
                    _RenderConn(child, y);
                }
            }
        }

        protected void _RerenderConn()
        {
            foreach (Connection conn in m_Owner.Conns.ConnectionsList)
            {
                double y = _CalcHorizontalPos(conn);
                foreach (Node child in conn)
                {
                    Path path;
                    if (!m_uiConns.TryGetValue(child, out path))
                        continue;

                    _DrawConnLine(path.Data as StreamGeometry, child, y);
                }
            }
        }
        protected void _RenderConn(Node child, double horizontalPos)
        {
            Path path = new Path();
            path.Stroke = Brushes.Black;
            path.StrokeThickness = 1;

            StreamGeometry geometry = new StreamGeometry();
            geometry.FillRule = FillRule.EvenOdd;

            _DrawConnLine(geometry, child, horizontalPos);

            path.Data = geometry;
            m_Panel.Children.Add(path);
            Panel.SetZIndex(path, -999);
            m_uiConns.Add(child, path);
        }

        protected void _DrawConnLine(StreamGeometry geometry, Node child, double horizontalPos)
        {
            if (child.ParentConn == null)
                return;
            UIConnector uiConn = GetConnector(child.ParentConn.Identifier);
            if (uiConn == null)
                return;
            UIConnector childConn = child.Renderer.GetConnector(Connection.IdentifierParent);
            if (childConn == null)
                return;

            Point parentPoint = uiConn.TransformToAncestor(m_Panel).Transform(new Point(uiConn.ActualWidth / 2, uiConn.ActualHeight / 2));
            Point childPoint = childConn.TransformToAncestor(m_Panel).Transform(new Point(childConn.ActualWidth / 2, childConn.ActualHeight / 2));

            using (StreamGeometryContext ctx = geometry.Open())
            {
                ctx.BeginFigure(parentPoint, false, false);

                ctx.LineTo(new Point(parentPoint.X, horizontalPos), true, false);

                ctx.LineTo(new Point(childPoint.X, horizontalPos), true, false);

                ctx.LineTo(childPoint, true, false);
            }
        }

        protected virtual void _RenderSelf()
        {
            _DrawFrame(m_Owner);
            _DrawName();
            _DrawSelfConnectors();
        }

        private void _DrawFrame(Node node)
        {
            m_Panel.Children.Add(m_uiFrame);

            Canvas.SetLeft(m_uiFrame, node.Geo.Pos.X);
            Canvas.SetTop(m_uiFrame, node.Geo.Pos.Y);

            m_uiFrame.MouseLeftButtonDown += MouseLeftButtonDown;
            m_uiFrame.MouseMove += MouseMove;
            m_uiFrame.MouseLeftButtonUp += MouseLeftButtonUp;
        }

        private void _DrawName()
        {
            m_uiFrame.name.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding("NickName") { Source = m_Owner });
        }

        private void _DrawSelfConnectors()
        {
            m_uiConnectors.Add(Connection.IdentifierParent, m_uiFrame.parentConnector);
            foreach (Connection conn in m_Owner.Conns.ConnectionsList)
            {
                if (conn is ConnectionNone)
                    continue;

                UIConnector uiConnector = new UIConnector();
                uiConnector.Title = conn.Identifier;

                m_uiFrame.bottomConnectors.Children.Add(uiConnector);

                m_uiConnectors.Add(conn.Identifier, uiConnector);
            }
        }

        Point pos = new Point();
        void MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            FrameworkElement tmp = (FrameworkElement)sender;
            pos = e.GetPosition(null);
            tmp.CaptureMouse();
            tmp.Cursor = Cursors.Hand;
        }
        void MouseMove(object sender, MouseEventArgs e)
        {
            FrameworkElement tmp = (FrameworkElement)sender;
            if (!tmp.IsMouseCaptured)
                return;
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                _Move(e.GetPosition(null) - pos);

                Node parent = m_Owner.Parent as Node;
                if (parent != null)
                {
                    parent.Renderer._RerenderConn();
                }
                pos = e.GetPosition(null);
            }
        }
        void MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            FrameworkElement tmp = (FrameworkElement)sender;
            tmp.ReleaseMouseCapture();
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
