using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace YBehavior.Editor.Core
{
    public class RenderersMgr
    {
        public static RenderersMgr Instance { get { return s_Instance; } }
        static RenderersMgr s_Instance = new RenderersMgr();

        private Dictionary<NodeType, Type> m_TypeDic = new Dictionary<NodeType, Type>();

        public RenderersMgr()
        {
            var subTypeQuery = from t in System.Reflection.Assembly.GetExecutingAssembly().GetTypes()
                               where IsSubClassOf(t, typeof(RendererBase))
                               select t;

            foreach (var type in subTypeQuery)
            {
                RendererBase node = Activator.CreateInstance(type) as RendererBase;
                if (node.Type == NodeType.NT_Invalid)
                    continue;
                m_TypeDic.Add(node.Type, type);
                Console.WriteLine(type);
            }
        }


        public RendererBase CreateRenderer(NodeType type)
        {
            if (m_TypeDic.TryGetValue(type, out Type renderertype))
            {
                return Activator.CreateInstance(renderertype) as RendererBase;
            }
            return null;
        }

        static bool IsSubClassOf(Type type, Type baseType)
        {
            var b = type.BaseType;
            while (b != null)
            {
                if (b.Equals(baseType))
                {
                    return true;
                }
                b = b.BaseType;
            }
            return false;
        }
    }


    public class RendererBase
    {
        protected string m_Name;
        public string Name { get { return m_Name; }}
        protected NodeType m_Type = NodeType.NT_Invalid;
        public NodeType Type { get { return m_Type; }}

        RectangleGeometry m_Frame = new RectangleGeometry();
        public virtual void Render(NodeBase node, Panel panel)
        {
            _DrawFrame(node, panel);
        }

        private void _DrawFrame(NodeBase node, Panel panel)
        {
            m_Frame.Rect = new System.Windows.Rect(node.Pos.X, node.Pos.Y, 80, 60);

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
                m_Frame.Rect = new System.Windows.Rect(
                    m_Frame.Rect.X + e.GetPosition(null).X - pos.X, 
                    m_Frame.Rect.Y + e.GetPosition(null).Y - pos.Y, 
                    80, 60);

                //Path tmp = (Path)sender;
                //double dx = e.GetPosition(null).X - pos.X + tmp.Margin.Left;
                //double dy = e.GetPosition(null).Y - pos.Y + tmp.Margin.Top;
                //tmp.Margin = new Thickness(dx, dy, 0, 0);
                pos = e.GetPosition(null);
            }
        }
        void MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Path tmp = (Path)sender;
            tmp.ReleaseMouseCapture();
        }
    }

    public class BranchRenderer : RendererBase
    {
        public override void Render(NodeBase node, Panel panel)
        {
            base.Render(node, panel);
            BranchNode branchNode = node as BranchNode;
            if (branchNode == null)
                return;
            foreach(var chi in branchNode.Children)
            {
                chi.Renderer.Render(chi, panel);
            }
        }
    }

    public class LeafRenderer : RendererBase
    {

    }

    public class SingleChildRenderer : BranchRenderer
    {

    }

    public class CompositeRenderer : BranchRenderer
    {

    }
}
