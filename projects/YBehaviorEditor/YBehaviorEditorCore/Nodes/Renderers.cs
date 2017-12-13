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

        RectangleGeometry m_Frame = new RectangleGeometry();
        Dictionary<Node, Path> m_Conns = new Dictionary<Node, Path>();

        public void Render(Panel panel)
        {
            _RenderSelf(panel);
            _RenderChildren(panel);
        }

        protected void _ClearConns(Panel panel)
        {
            foreach (var pair in m_Conns)
            {
                panel.Children.Remove(pair.Value);
            }
            m_Conns.Clear();
        }
        protected double _CalcHorizontalPos(Connection conn)
        {
            double y = 0;
            double miny = double.MaxValue;
            foreach (Node child in conn)
            {
                double top = child.Geo.Rec.Top;
                y += top;
                miny = Math.Min(miny, top);
            }
            y /= conn.NodeCount;
            y -= 10;
            miny -= 10;
            y = Math.Min(miny, y);
            y = Math.Max(y, m_Owner.Geo.Rec.Bottom + 10);
            return y;
        }
        protected void _RenderChildren(Panel panel)
        {
            _ClearConns(panel);
            foreach (Connection conn in m_Owner.Conns.ConnectionsList)
            {
                double y = _CalcHorizontalPos(conn);
                foreach (Node child in conn)
                {
                    child.Renderer.Render(panel);
                    _RenderConn(panel, child, y);
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
                    if (!m_Conns.TryGetValue(child, out path))
                        continue;

                    _DrawConnLine(path.Data as StreamGeometry, m_Owner.Geo, child.Geo, y);
                }
            }
        }
        protected void _RenderConn(Panel panel, Node child, double horizontalPos)
        {
            Path path = new Path();
            path.Stroke = Brushes.Black;
            path.StrokeThickness = 1;

            StreamGeometry geometry = new StreamGeometry();
            geometry.FillRule = FillRule.EvenOdd;

            _DrawConnLine(geometry, m_Owner.Geo, child.Geo, horizontalPos);

            path.Data = geometry;
            panel.Children.Add(path);
            Panel.SetZIndex(path, -999);
            m_Conns.Add(child, path);
        }

        protected void _DrawConnLine(StreamGeometry geometry, Node.Geometry parent, Node.Geometry child, double horizontalPos)
        {
            using (StreamGeometryContext ctx = geometry.Open())
            {
                ctx.BeginFigure(parent.BottomPoint, false, false);

                ctx.LineTo(new Point(parent.BottomPoint.X, horizontalPos), true, false);

                ctx.LineTo(new Point(child.TopPoint.X, horizontalPos), true, false);

                ctx.LineTo(child.TopPoint, true, false);
            }
        }

        protected virtual void _RenderSelf(Panel panel)
        {
            _DrawFrame(m_Owner, panel);
        }

        private void _DrawFrame(Node node, Panel panel)
        {
            m_Frame.Rect = node.Geo.Rec;

            Path path = new Path();
            path.Fill = Brushes.LemonChiffon;
            path.Stroke = Brushes.Black;
            path.StrokeThickness = 1;
            path.Data = m_Frame;
            panel.Children.Add(path);

            path.MouseLeftButtonDown += MouseLeftButtonDown;
            path.MouseMove += MouseMove;
            path.MouseLeftButtonUp += MouseLeftButtonUp;
        }

        Point pos = new Point();
        void MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Path tmp = (Path)sender;
            pos = e.GetPosition(null);
            tmp.CaptureMouse();
            tmp.Cursor = Cursors.Hand;
        }
        void MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                m_Owner.Geo.Pos = m_Owner.Geo.Pos + (e.GetPosition(null) - pos);
                m_Frame.Rect = m_Owner.Geo.Rec;

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
            Path tmp = (Path)sender;
            tmp.ReleaseMouseCapture();
        }

    }
}
