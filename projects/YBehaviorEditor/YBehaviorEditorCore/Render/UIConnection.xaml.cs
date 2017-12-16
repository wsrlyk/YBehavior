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

namespace YBehavior.Editor.Core
{
    /// <summary>
    /// UIConnection.xaml 的交互逻辑
    /// </summary>
    public partial class UIConnection : UserControl, ISelectable
    {
        static SelectionStateChangeHandler defaultSelectHandler = new SelectionStateChangeHandler(SelectionMgr.Instance.OnSingleSelectedChange);

        PathFigure figure;
        Brush normalStrokeBrush;
        SelectionStateChangeHandler SelectHandler { get; set; }
        Operation m_Operation;

        public UIConnection()
        {
            InitializeComponent();
            figure = PathGeometry.Figures[0];
            Clear();
            normalStrokeBrush = this.path.Stroke;

            m_Operation = new Operation(this);
            m_Operation.RegisterClick(_OnClick);
        }

        public PathGeometry PathGeometry { get { return path.Data as PathGeometry; } }

        public void Clear()
        {
            figure.Segments.Clear();
            figure.StartPoint = new Point();
        }

        public void SetWithMidY(Point start, Point end, double midY)
        {
            Clear();
            figure.StartPoint = start;
            LineSegment fstLine = new LineSegment();
            fstLine.Point = new Point(start.X, midY);
            LineSegment secLine = new LineSegment();
            secLine.Point = new Point(end.X, midY);
            LineSegment trdLine = new LineSegment();
            trdLine.Point = end;
            figure.Segments.Add(fstLine);
            figure.Segments.Add(secLine);
            figure.Segments.Add(trdLine);
        }

        void _OnClick()
        {
            if (SelectHandler != null)
                SelectHandler(this, true);
            else
                defaultSelectHandler(this, true);
        }

        public void SetSelect(bool bSelect)
        {
            if (bSelect)
                this.path.Stroke = new SolidColorBrush(Colors.DarkBlue);
            else
                this.path.Stroke = normalStrokeBrush;
        }
    }
}
