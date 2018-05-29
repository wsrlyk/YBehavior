using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using YBehavior.Editor.Core;

namespace YBehavior.Editor
{
    /// <summary>
    /// UIConnection.xaml 的交互逻辑
    /// </summary>
    public partial class UIConnection : YUserControl, ISelectable, IDeletable
    {
        static SelectionStateChangeHandler defaultSelectHandler = new SelectionStateChangeHandler(SelectionMgr.Instance.OnSingleSelectedChange);
        //static SelectionStateChangeHandler defaultSelectHandler = new SelectionStateChangeHandler(SelectionMgr.Instance.OnSingleSelectedChange);

        PathFigure figure;
        Brush normalStrokeBrush;

        SelectionStateChangeHandler SelectHandler { get; set; }
        Operation m_Operation;

        public ConnectionHolder ChildHolder { get; set; }

        public UIConnection()
        {
            InitializeComponent();
            figure = PathGeometry.Figures[0];
            //Clear();
            normalStrokeBrush = this.path.Stroke;

            SelectHandler = new SelectionStateChangeHandler(defaultSelectHandler);

            m_Operation = new Operation(this);
            m_Operation.RegisterClick(_OnClick);
            this.DataContextChanged += _DataContextChangedEventHandler;
        }

        void _DataContextChangedEventHandler(object sender, DependencyPropertyChangedEventArgs e)
        {
            ConnectionRenderer renderer = this.DataContext as ConnectionRenderer;
            ChildHolder = renderer.ChildConn;

            //SetCanvas((renderer.ChildConn.Owner as Node).Renderer.RenderCanvas);
        }

        protected override void _OnAncestorPropertyChanged()
        {
            m_Operation.RenderCanvas.Panel = Ancestor;
        }

        public void SetCanvas(RenderCanvas canvas)
        {
            m_Operation.SetCanvas(canvas);
        }

        public PathGeometry PathGeometry { get { return path.Data as PathGeometry; } }

        //public void Clear()
        //{
        //    figure.Segments.Clear();
        //    figure.StartPoint = new Point();
        //}

        public void SetWithMidY(Point start, Point end, double midY)
        {
            //Clear();
            figure.StartPoint = start;

            LineSegment fstLine = figure.Segments[0] as LineSegment;
            fstLine.Point = new Point(start.X, midY);
            LineSegment secLine = figure.Segments[1] as LineSegment;
            secLine.Point = new Point(end.X, midY);
            LineSegment trdLine = figure.Segments[2] as LineSegment;
            trdLine.Point = end;

        }

        void _OnClick()
        {
            SelectHandler(this, true);
            m_Operation.MakeCanvasFocused();
        }

        public void SetSelect(bool bSelect)
        {
            if (bSelect)
                this.path.Stroke = new SolidColorBrush(Colors.DarkBlue);
            else
                this.path.Stroke = normalStrokeBrush;
        }

        public void OnDelete(int param)
        {
            if (DebugMgr.Instance.IsDebugging())
                return;
            NodesDisconnectedArg arg = new NodesDisconnectedArg();
            arg.ChildHolder = this.ChildHolder;
            EventMgr.Instance.Send(arg);
        }
    }
}
