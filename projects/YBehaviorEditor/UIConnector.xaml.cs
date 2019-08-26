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
    public partial class UIConnector : YUserControl, IDragable, IDropable
    {
        static DragHandler defaultDragHandler = new DragHandler(DragDropMgr.Instance.OnDragged);
        static DropHandler defaultDropHandler = new DropHandler(DragDropMgr.Instance.OnDropped);
        static DropHandler defaultHoverHandler = new DropHandler(DragDropMgr.Instance.OnHover);

        DragHandler DragHandler { get; set; }
        DropHandler DropHandler { get; set; }
        DropHandler HoverHandler { get; set; }

        Brush normalBorderBrush;

        Operation m_Operation;

        #region Dependency Property/Event Definitions

        public static readonly DependencyProperty HotspotProperty =
            DependencyProperty.Register("Hotspot", typeof(Point), typeof(UIConnector));
        public static readonly DependencyProperty OwnerNodeProperty =
            DependencyProperty.Register("OwnerNode", typeof(UINode), typeof(UIConnector));

        #endregion Dependency Property/Event Definitions

        public Point Hotspot
        {
            get
            {
                return (Point)GetValue(HotspotProperty);
            }
            set
            {
                SetValue(HotspotProperty, value);
            }
        }

        public YUserControl OwnerNode
        {
            get
            {
                return (YUserControl)GetValue(OwnerNodeProperty);
            }
            set
            {
                SetValue(OwnerNodeProperty, value);
            }
        }

        Vector m_RelativePos = new Vector(double.MaxValue, double.MaxValue);

        public UIConnector()
        {
            InitializeComponent();

            normalBorderBrush = this.Main.BorderBrush;

            DragHandler = new DragHandler(defaultDragHandler);
            DropHandler = new DropHandler(defaultDropHandler);
            HoverHandler = new DropHandler(defaultHoverHandler);

            m_Operation = new Operation(this);
            m_Operation.RegisterDragDrop(_OnDragged, _OnStartDragged, _OnFinishDragged);

            this.LayoutUpdated += _OnLayoutUpdated;

            this.SetBinding(OwnerNodeProperty, new Binding()
            {
                RelativeSource = new RelativeSource()
                {
                    Mode = RelativeSourceMode.FindAncestor,
                    AncestorLevel = 1,
                    AncestorType = typeof(UINode)
                }
            });
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

        public void ResetPos()
        {
            m_RelativePos.X = double.MaxValue;
            m_RelativePos.Y = double.MaxValue;
        }

        public string Title
        {
            get { return title.Text; }
            set { title.Text = value; }
        }

        public Connector Ctr { get; set; }

        public Point GetPos(Visual visual)
        {
            if (visual == null)
                return new Point();
            try
            {
                return TransformToAncestor(visual).Transform(new Point(ActualWidth / 2, ActualHeight / 2));
            }
            catch (Exception e)
            {
                LogMgr.Instance.Log(e.ToString());
                return new Point();
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
                Point from = GetPos(DraggingConnection.Instance.Canvas);
                Point to = absPos;
                DraggingConnection.Instance.Drag(from, to);

                HoverHandler(droppable);
            }
        }
        void _OnFinishDragged(Vector delta, Point absPos)
        {
            if (DebugMgr.Instance.IsDebugging())
                return;
            IDropable droppable = _HitTesting(absPos);
            DraggingConnection.Instance.FinishDrag();

            DropHandler(droppable);
        }

        IDropable _HitTesting(Point pos)
        {
            List<DependencyObject> objs = m_Operation.HitTesting(pos);
            foreach (var o in objs)
            {
                var obj = o;
                //                LogMgr.Instance.Log("Drag HitTest: " + obj.ToString());
                while (obj != null && obj.GetType() != typeof(Canvas))
                {
                    if (obj is IDropable && obj != this)
                    {
                        //                        LogMgr.Instance.Log("Drag FinalHit: " + obj.ToString());
                        return obj as IDropable;
                    }

                    obj = VisualTreeHelper.GetParent(obj);
                }
            }
            //            LogMgr.Instance.Log("Drag FinalHit: NULL");
            return null;
        }

        public void SetDragged(bool bDragged)
        {
            if (bDragged)
                this.Main.BorderBrush = new SolidColorBrush(Colors.Magenta);
            else
                this.Main.BorderBrush = normalBorderBrush;
        }

        public void SetDropped(bool bDropped)
        {
            if (bDropped)
                this.Main.BorderBrush = new SolidColorBrush(Colors.Chocolate);
            else
                this.Main.BorderBrush = normalBorderBrush;
        }

        public void OnDropped(IDragable dragable)
        {
            if (DebugMgr.Instance.IsDebugging())
                return;

            if (dragable == null)
                return;

            UIConnector other = dragable as UIConnector;
            if (other == null)
                return;

            WorkBenchMgr.Instance.ConnectNodes(this.Ctr, other.Ctr);
        }
    }
}
