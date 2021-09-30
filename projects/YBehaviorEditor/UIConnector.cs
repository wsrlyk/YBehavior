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
    public abstract class UIConnector : YUserControl
    {
        protected static DragHandler defaultDragHandler = new DragHandler(DragDropMgr.Instance.OnDragged);
        protected static DropHandler defaultDropHandler = new DropHandler(DragDropMgr.Instance.OnDropped);
        protected static DropHandler defaultHoverHandler = new DropHandler(DragDropMgr.Instance.OnHover);

        protected DragHandler DragHandler { get; set; }
        protected DropHandler DropHandler { get; set; }
        protected DropHandler HoverHandler { get; set; }

        protected Operation m_Operation;

        #region Dependency Property/Event Definitions

        public static readonly DependencyProperty HotspotProperty =
            DependencyProperty.Register("Hotspot", typeof(Point), typeof(UIConnector));
        public static readonly DependencyProperty OwnerNodeProperty =
            DependencyProperty.Register("OwnerNode", typeof(YUserControl), typeof(UIConnector));

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

        public abstract Border Main { get; }

        ////Vector m_RelativePos = new Vector(double.MaxValue, double.MaxValue);

        public UIConnector()
        {
            this.LayoutUpdated += _OnLayoutUpdated;

            this.SetBinding(OwnerNodeProperty, new Binding()
            {
                RelativeSource = new RelativeSource()
                {
                    Mode = RelativeSourceMode.FindAncestor,
                    AncestorLevel = 1,
                    AncestorType = typeof(YUserControl)
                }
            });
        }

        void _OnLayoutUpdated(object sender, EventArgs e)
        {
            _UpdateHotspot();
        }

        private void _UpdateHotspot()
        {
            if (OwnerNode != null && OwnerNode.Canvas.IsAncestorOf(this))
            {
                ////if (m_RelativePos.X == double.MaxValue && m_RelativePos.Y == double.MaxValue)
                ////{
                ////    m_RelativePos = TransformToAncestor(OwnerNode).Transform(new Point(ActualWidth / 2, ActualHeight / 2)) - new Point(OwnerNode.ActualWidth / 2, OwnerNode.ActualHeight / 2 - ActualHeight);
                ////}

                ////if (OwnerNode.DataContext is NodeBaseRenderer)
                ////{
                ////    Point pos = (OwnerNode.DataContext as NodeBaseRenderer).Owner.Geo.Pos + m_RelativePos;
                ////    //Hotspot = pos;
                ////    (this.DataContext as ConnectorGeometry).Pos = pos;
                ////}
                (this.DataContext as ConnectorRenderer).Owner.Geo.Pos = TransformToAncestor(OwnerNode.Canvas).Transform(new Point(ActualWidth / 2, ActualHeight / 2));
            }

            //Hotspot = GetPos(Ancestor);
        }

        //public void ResetPos()
        //{
        //    m_RelativePos.X = double.MaxValue;
        //    m_RelativePos.Y = double.MaxValue;
        //}

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

        protected IDropable _HitTesting(Point pos)
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
    }
}
