﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using YBehavior.Editor.Core.New;

namespace YBehavior.Editor
{
    /// <summary>
    /// UIConnection.xaml 的交互逻辑
    /// </summary>

    public abstract class UIConnectionBase : DebugControl<UIConnectionBase>
    {
        public UIConnectionBase()
        { }

        public UIConnectionBase(bool bHasOperation)
            :base (bHasOperation)
        { }
    }

    public partial class UIConnection : UIConnectionBase, ISelectable, IDeletable, IDraggingConnection
    {
        static SelectionStateChangeHandler defaultSelectHandler = new SelectionStateChangeHandler(SelectionMgr.Instance.OnSingleSelectedChange);
        //static SelectionStateChangeHandler defaultSelectHandler = new SelectionStateChangeHandler(SelectionMgr.Instance.OnSingleSelectedChange);

        PathFigure figure;
        Brush normalStrokeBrush;

        SelectionStateChangeHandler SelectHandler { get; set; }
        Operation m_Operation;

        public override FrameworkElement DebugUI { get { return this.debug; } }
        public override Brush DebugBrush
        {
            get { return this.debug.Stroke; }
            set
            {
                this.debug.Stroke = value;
            }
        }
        Storyboard m_InstantAnim;
        public override Storyboard InstantAnim { get { return m_InstantAnim; } }

        public override NodeState RunState => m_Renderer.RunState;
        ConnectionRenderer m_Renderer;

        public UIConnection()
        {
            InitializeComponent();
            figure = PathGeometry.Figures[0];
            //Clear();
            normalStrokeBrush = this.path.Stroke;

            m_InstantAnim = Application.Current.Resources["InstantShowAnim"] as Storyboard;

            SelectHandler = new SelectionStateChangeHandler(defaultSelectHandler);

            m_Operation = new Operation(this);
            m_Operation.RegisterLeftClick(_OnClick);
            this.DataContextChanged += _DataContextChangedEventHandler;

            this.SetBinding(DebugTriggerProperty, new Binding()
            {
                Path = new PropertyPath("DebugTrigger"),
                Mode = BindingMode.OneWay,
            });
        }

        public UIConnection(bool bHasOperation)
            : base (bHasOperation)
        {
            InitializeComponent();
            figure = PathGeometry.Figures[0];
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
            m_Renderer = this.DataContext as ConnectionRenderer;

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

    }
}
