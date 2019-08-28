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
using YBehavior.Editor.Core.New;

namespace YBehavior.Editor
{
    /// <summary>
    /// UIConnection.xaml 的交互逻辑
    /// </summary>
    public partial class FSMUIConnection : YUserControl, ISelectable, IDeletable, IDraggingConnection
    {
        static SelectionStateChangeHandler defaultSelectHandler = new SelectionStateChangeHandler(SelectionMgr.Instance.OnSingleSelectedChange);
        //static SelectionStateChangeHandler defaultSelectHandler = new SelectionStateChangeHandler(SelectionMgr.Instance.OnSingleSelectedChange);

        PathFigure figure;
        PathFigure arrowFigure;
        Brush normalStrokeBrush;

        SelectionStateChangeHandler SelectHandler { get; set; }
        Operation m_Operation;

        public FSMUIConnection()
        {
            InitializeComponent();
            figure = PathGeometry.Figures[0];
            arrowFigure = PathGeometry.Figures[1];
            //Clear();
            normalStrokeBrush = this.path.Stroke;

            SelectHandler = new SelectionStateChangeHandler(defaultSelectHandler);

            m_Operation = new Operation(this);
            m_Operation.RegisterLeftClick(_OnClick);
            this.DataContextChanged += _DataContextChangedEventHandler;
        }

        public FSMUIConnection(bool bHasOperation)
            : base (bHasOperation)
        {
            InitializeComponent();
            figure = PathGeometry.Figures[0];
            arrowFigure = PathGeometry.Figures[1];
            //Clear();
            normalStrokeBrush = this.path.Stroke;

            if (bHasOperation)
            {
                SelectHandler = new SelectionStateChangeHandler(defaultSelectHandler);

                m_Operation = new Operation(this);
                m_Operation.RegisterLeftClick(_OnClick);
                this.DataContextChanged += _DataContextChangedEventHandler;
            }
        }

        void _DataContextChangedEventHandler(object sender, DependencyPropertyChangedEventArgs e)
        {
            //ConnectionRenderer renderer = this.DataContext as ConnectionRenderer;

            //SetCanvas((renderer.ChildConn.Owner as Node).Renderer.RenderCanvas);
        }

        public PathGeometry PathGeometry { get { return path.Data as PathGeometry; } }

        //public void Clear()
        //{
        //    figure.Segments.Clear();
        //    figure.StartPoint = new Point();
        //}

        public void Set(Point start, Point end, double midY)
        {
            //Clear();
            figure.StartPoint = start;

            LineSegment trdLine = figure.Segments[0] as LineSegment;
            trdLine.Point = end;

            _UpdateArrow();
        }

        void _OnClick()
        {
            SelectHandler(this, true);
            m_Operation.MakeCanvasFocused();
        }

        public void SetSelect(bool bSelect)
        {
            if (bSelect)
                this.path.SetResourceReference(Shape.StrokeProperty, SystemColors.HotTrackBrushKey);
            else
                this.path.Stroke = normalStrokeBrush;
        }

        public void OnDelete(int param)
        {
            if (DebugMgr.Instance.IsDebugging())
                return;

            WorkBenchMgr.Instance.DisconnectNodes((this.DataContext as ConnectionRenderer).Owner.Ctr);
        }

        private void Path_TargetUpdated(object sender, DataTransferEventArgs e)
        {
            _UpdateArrow();
        }

        void _UpdateArrow()
        {
            Point start = figure.StartPoint;

            LineSegment trdLine = figure.Segments[0] as LineSegment;
            Point end = trdLine.Point;

            Point midPoint = new Point((start.X + end.X) * 0.5, (start.Y + end.Y) * 0.5);
            Vector arrow = end - start;
            arrow.Normalize();

            arrowFigure.StartPoint = midPoint + arrow * 2;

            midPoint = midPoint - arrow * 2;

            arrow = new Vector(arrow.Y, -arrow.X);
            (arrowFigure.Segments[0] as LineSegment).Point = midPoint + arrow * 2;
            arrow = -arrow;
            (arrowFigure.Segments[1] as LineSegment).Point = midPoint + arrow * 2;
        }
    }
}
