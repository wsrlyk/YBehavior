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
    /// UIConnector.xaml 的交互逻辑
    /// </summary>
    public partial class VariableUIConnector : UIConnector, IDragable, IDropable
    {
        Brush normalBorderBrush;

        public override Border Main { get { return this.main; } }

        Vector m_RelativePos = new Vector(double.MaxValue, double.MaxValue);

        public VariableUIConnector()
        {
            InitializeComponent();

            m_Operation = new Operation(this);

            normalBorderBrush = this.Main.BorderBrush;

            DragHandler = new DragHandler(defaultDragHandler);
            DropHandler = new DropHandler(defaultDropHandler);
            HoverHandler = new DropHandler(defaultHoverHandler);

            m_Operation.RegisterLeftDrag(_OnDragged, _OnStartDragged, _OnFinishDragged);
        }

        void _OnLayoutUpdated(object sender, EventArgs e)
        {
            _UpdateHotspot();
        }

        private void _UpdateHotspot()
        {
            if (OwnerNode != null)
            {
                if (m_RelativePos.X == double.MaxValue && m_RelativePos.Y == double.MaxValue)
                {
                    m_RelativePos = TransformToAncestor(OwnerNode).Transform(new Point(ActualWidth / 2, ActualHeight / 2)) - new Point();
                }

                if (OwnerNode.DataContext is NodeBaseRenderer)
                {
                    Point pos = (OwnerNode.DataContext as NodeBaseRenderer).Owner.Geo.Pos + m_RelativePos;
                    //Hotspot = pos;
                    (this.DataContext as ConnectorGeometry).Pos = pos;
                }
            }

            //Hotspot = GetPos(Ancestor);
        }

        public string Title
        {
            get { return title.Text; }
            set { title.Text = value; }
        }

        public bool Left
        {
            set
            {
                if (value)
                    main.HorizontalAlignment = HorizontalAlignment.Left;
                else
                    main.HorizontalAlignment = HorizontalAlignment.Right;
            }
        }

        void _OnStartDragged(Vector delta, Point absPos)
        {
            if (DebugMgr.Instance.IsDebugging())
                return;
            DragHandler(this, true);
        }

        void _OnDragged(Vector delta, Point absPos)
        {
            if (DebugMgr.Instance.IsDebugging())
                return;
            IDropable droppable = _HitTesting(absPos);

            {
                Point from = GetPos(DraggingConnection<UIDragConnection>.Instance.Canvas);
                Point to = absPos;
                DraggingConnection<UIDragConnection>.Instance.Drag(from, to);

                HoverHandler(droppable);
            }
        }
        void _OnFinishDragged(Vector delta, Point absPos)
        {
            if (DebugMgr.Instance.IsDebugging())
                return;
            IDropable droppable = _HitTesting(absPos);
            DraggingConnection<UIDragConnection>.Instance.FinishDrag();

            DropHandler(droppable);
        }


        public void SetDragged(bool bDragged)
        {
            if (bDragged)
                this.Main.BorderBrush = App.Current.FindResource("ConnectorOut") as SolidColorBrush;
            else
                this.Main.BorderBrush = normalBorderBrush;
        }

        public void SetDropped(bool bDropped)
        {
            if (bDropped)
                this.Main.BorderBrush = App.Current.FindResource("ConnectorIn") as SolidColorBrush;
            else
                this.Main.BorderBrush = normalBorderBrush;
        }

        public void OnDropped(IDragable dragable)
        {
            if (DebugMgr.Instance.IsDebugging())
                return;

            if (dragable == null)
                return;

            VariableUIConnector other = dragable as VariableUIConnector;
            if (other == null)
                return;

            (WorkBenchMgr.Instance.ActiveWorkBench as TreeBench).ConnectVariables(this.Ctr, other.Ctr);
        }
    }
}
