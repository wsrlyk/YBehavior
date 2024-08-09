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
    /// UI of fsm connection line
    /// </summary>
    public partial class FSMUIConnection : YUserControl, ISelectable, IDeletable, IDraggingConnection
    {
        static SelectionStateChangeHandler defaultSelectHandler = new SelectionStateChangeHandler(SelectionMgr.Instance.OnSingleSelectedChange);
        //static SelectionStateChangeHandler defaultSelectHandler = new SelectionStateChangeHandler(SelectionMgr.Instance.OnSingleSelectedChange);

        PathFigure figure;
        Brush normalStrokeBrush;

        PathFigure[] arrowFigure = new PathFigure[3];

        SelectionStateChangeHandler SelectHandler { get; set; }
        Operation m_Operation;
        FSMConnection Conn;
        public FSMUIConnection()
        {
            InitializeComponent();
            figure = PathGeometry.Figures[0];
            arrowFigure[0] = PathGeometry.Figures[1];
            arrowFigure[1] = (extraArrows.Data as PathGeometry).Figures[0];
            arrowFigure[2] = (extraArrows.Data as PathGeometry).Figures[1];
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
            arrowFigure[0] = PathGeometry.Figures[1];
            arrowFigure[1] = (extraArrows.Data as PathGeometry).Figures[0];
            arrowFigure[2] = (extraArrows.Data as PathGeometry).Figures[1];
            extraArrows.Visibility = Visibility.Collapsed;
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
            Conn = (this.DataContext as FSMConnectionRenderer).FSMOwner;

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

        void _OnClick(Point pos)
        {
            SelectHandler(this, true);
            m_Operation.MakeCanvasFocused();
        }

        public void SetSelect(bool bSelect)
        {
            if (bSelect)
            {
                this.path.SetResourceReference(Shape.StrokeProperty, SystemColors.HotTrackBrushKey);
                this.extraArrows.SetResourceReference(Shape.StrokeProperty, SystemColors.HotTrackBrushKey);
            }
            else
            {
                this.path.Stroke = normalStrokeBrush;
                this.extraArrows.Stroke = normalStrokeBrush;
            }
        }

        public void OnDelete(int param)
        {
            if (NetworkMgr.Instance.IsConnected)
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

            _DrawArrow(arrowFigure[0], start, end, 0);
            _DrawArrow(arrowFigure[1], start, end, 0.06f);
            _DrawArrow(arrowFigure[2], start, end, -0.06f);
        }

        void _DrawArrow(PathFigure figure, Point start, Point end, float offset)
        {
            Vector arrow = end - start;

            Point midPoint = start + arrow * (0.47f + offset);

            arrow.Normalize();

            figure.StartPoint = midPoint + arrow * 2;

            midPoint = midPoint - arrow * 2;

            arrow = new Vector(arrow.Y, -arrow.X);
            (figure.Segments[0] as LineSegment).Point = midPoint + arrow * 2;
            arrow = -arrow;
            (figure.Segments[1] as LineSegment).Point = midPoint + arrow * 2;
        }
    }
}
