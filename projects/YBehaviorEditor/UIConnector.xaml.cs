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

        public UIConnector()
        {
            InitializeComponent();

            normalBorderBrush = this.Main.BorderBrush;

            DragHandler = new DragHandler(defaultDragHandler);
            DropHandler = new DropHandler(defaultDropHandler);
            HoverHandler = new DropHandler(defaultHoverHandler);

            m_Operation = new Operation(this);
            m_Operation.RegisterDragDrop(_OnDragged, _OnStartDragged);

            this.LayoutUpdated += _OnLayoutUpdated;
        }

        void _OnLayoutUpdated(object sender, EventArgs e)
        {
            _UpdateHotspot();
        }

        private void _UpdateHotspot()
        {
            if (this.Ancestor != null && !this.Ancestor.IsAncestorOf(this))
            {
                this.Ancestor = null;
                return;
            }

            if (Ancestor == null)
                return;

            Hotspot = GetPos(Ancestor);
        }

        public string Title
        {
            get { return title.Text; }
            set { title.Text = value; }
        }

        public ConnectionHolder ConnHolder { get; set; }

        public Point GetPos(Visual visual)
        {
            if (visual == null)
                return new Point();
            try
            {
                return TransformToAncestor(visual).Transform(new Point(ActualWidth / 2, ActualHeight / 2));
            }
            catch(Exception e)
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

            ///> DragFinish
            if (delta.LengthSquared == 0)
            {
                DraggingConnection.Instance.FinishDrag();

                DropHandler(droppable);
            }
            else
            {
                Point from = GetPos(DraggingConnection.Instance.Canvas);
                Point to = absPos;
                DraggingConnection.Instance.Drag(from, to);

                HoverHandler(droppable);
            }
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

            NodesConnectedArg arg = new NodesConnectedArg();
            arg.Holder0 = this.ConnHolder;
            arg.Holder1 = other.ConnHolder;
            EventMgr.Instance.Send(arg);
        }
    }
}
