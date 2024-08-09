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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using YBehavior.Editor.Core.New;

namespace YBehavior.Editor
{
    /// <summary>
    /// UI of dragging line
    /// </summary>
    public partial class UIDragConnection : YUserControl, IDraggingConnection
    {
        PathFigure figure;
        public PathGeometry PathGeometry { get { return path.Data as PathGeometry; } }
        public UIDragConnection()
        {
            InitializeComponent();
            figure = PathGeometry.Figures[0];
        }

        public UIDragConnection(bool bHasOperation)
              : base(bHasOperation)
        {
            InitializeComponent();
            figure = PathGeometry.Figures[0];
        }

        public void Set(Point start, Point end, double midY)
        {
            //Clear();
            figure.StartPoint = start;

            LineSegment trdLine = figure.Segments[0] as LineSegment;
            trdLine.Point = end;

        }

    }
}
