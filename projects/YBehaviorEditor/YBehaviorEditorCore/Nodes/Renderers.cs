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

        Border m_uiFrame = new Border();
        Dictionary<Node, Path> m_uiConns = new Dictionary<Node, Path>();
        Dictionary<Connection, Border> m_uiConnectors = new Dictionary<Connection, Border>();
        Label m_uiName = new Label();

        public void Render(Panel panel)
        {
            _RenderSelf(panel);
            _RenderChildren(panel);
        }

        protected void _ClearConns(Panel panel)
        {
            foreach (var pair in m_uiConns)
            {
                panel.Children.Remove(pair.Value);
            }
            m_uiConns.Clear();
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
                    if (!m_uiConns.TryGetValue(child, out path))
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
            m_uiConns.Add(child, path);
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
            _DrawName();
        }

        private void _DrawFrame(Node node, Panel panel)
        {
            panel.Children.Add(m_uiFrame);
            m_uiFrame.SetBinding(Border.WidthProperty, new System.Windows.Data.Binding("Width") { Source = node.Geo });// = node.Geo.Rec.Width;
            m_uiFrame.SetBinding(Border.HeightProperty, new System.Windows.Data.Binding("Height") { Source = node.Geo });// = node.Geo.Rec.Width;
            //m_uiFrame.Height = node.Geo.Rec.Height;
            m_uiFrame.BorderBrush = Brushes.Honeydew;
            m_uiFrame.BorderThickness = new Thickness(3);
            m_uiFrame.Background = Brushes.Gray;
            StackPanel stackPanel = new StackPanel();
            m_uiFrame.Child = stackPanel;
            stackPanel.HorizontalAlignment = HorizontalAlignment.Center;

            Canvas.SetLeft(m_uiFrame, node.Geo.Pos.X);
            Canvas.SetTop(m_uiFrame, node.Geo.Pos.Y);

            //Path path = new Path();
            //path.Fill = Brushes.LemonChiffon;
            //path.Stroke = Brushes.Black;
            //path.StrokeThickness = 1;
            //path.Data = m_uiFrame;
            //panel.Children.Add(path);

            m_uiFrame.MouseLeftButtonDown += MouseLeftButtonDown;
            m_uiFrame.MouseMove += MouseMove;
            m_uiFrame.MouseLeftButtonUp += MouseLeftButtonUp;
        }

        private void _DrawName()
        {
            m_uiName.Content = m_Owner.NickName;
            StackPanel p = m_uiFrame.Child as StackPanel;
            p.Children.Add(m_uiName);
        }

        private void _DrawSelfConnectors()
        {
            //foreach (Connection conn in m_Owner.Conns.ConnectionsList)
            //{
            //    if (conn is ConnectionNone)
            //        continue;
            //    if (conn.Identifier == Connection.IdentifierChildren)
            //    {

            //    }

            //    Border border = new Border();
            //    m_uiFrame.
            //}
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
            m_Owner.Geo.Width = 20 + (GetTextDisplayWidthHelper.GetTextDisplayWidth(m_uiName, m_Owner.NickName));
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

    static class GetTextDisplayWidthHelper
    {


        public static Double GetTextDisplayWidth(Label label, string content)
        {
            return GetTextDisplayWidth(content, label.FontFamily, label.FontStyle, label.FontWeight, label.FontStretch, label.FontSize);
        }


        public static Double GetTextDisplayWidth(string str, FontFamily fontFamily, FontStyle fontStyle, FontWeight fontWeight, FontStretch fontStretch, double FontSize)
        {
            var formattedText = new FormattedText(
                                str,
                                System.Globalization.CultureInfo.CurrentUICulture,
                                FlowDirection.LeftToRight,
                                new Typeface(fontFamily, fontStyle, fontWeight, fontStretch),
                                FontSize,
                                Brushes.Black
                                );
            Size size = new Size(formattedText.Width, formattedText.Height);
            return size.Width;
        }
    }
}
