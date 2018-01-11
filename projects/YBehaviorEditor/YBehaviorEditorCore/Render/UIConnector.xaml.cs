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
    /// UIConnector.xaml 的交互逻辑
    /// </summary>
    public partial class UIConnector : UserControl, IDragable, IDropable
    {
        static DragHandler defaultDragHandler = new DragHandler(DragDropMgr.Instance.OnDragged);
        static DropHandler defaultDropHandler = new DropHandler(DragDropMgr.Instance.OnDropped);

        DragHandler DragHandler { get; set; }
        DropHandler DropHandler { get; set; }

        Brush normalBorderBrush;

        Operation m_Operation;

        public UIConnector()
        {
            InitializeComponent();
            //this.DragLeave += _OnDragged;
            this.DragEnter += _OnDropped;
            normalBorderBrush = this.BorderBrush;
        }

        public void SetCanvas(Panel panel)
        {
            m_Operation = new Operation(this, panel);
            m_Operation.RegisterDrag(_OnDragged);
        }

        public string Title
        {
            get { return title.Text; }
            set { title.Text = value; }
        }

        public ConnectionHolder ConnHolder { get; set; }

        public Point GetPos(Visual visual)
        {
            return TransformToAncestor(visual).Transform(new Point(ActualWidth / 2, ActualHeight / 2));
        }

        void _OnDragged(object sender, DragEventArgs e)
        {
            if (DragHandler != null)
                DragHandler(this, true);
            else
                defaultDragHandler(this, true);
        }
        void _OnDropped(object sender, DragEventArgs e)
        {
            if (DropHandler != null)
                DropHandler(this);
            else
                defaultDropHandler(this);
        }

        void _OnDragged(Vector delta, Point absPos)
        {
            ///> DragFinish
            if (delta.LengthSquared == 0 && absPos.X == 0 && absPos.Y == 0)
            {
                DraggingConnection.Instance.FinishDrag();
            }
            else
            {
                Point from = GetPos(DraggingConnection.Instance.Canvas);
                Point to = absPos;
                DraggingConnection.Instance.Drag(from, to);

                DataObject data = new DataObject();
                data.SetData(DataFormats.StringFormat, this.ToString());
                DragDrop.DoDragDrop(this, data, DragDropEffects.None);
            }
        }

        public void SetDragged(bool bDragged)
        {
            if (bDragged)
                this.BorderBrush = new SolidColorBrush(Colors.Cyan);
            else
                this.BorderBrush = normalBorderBrush;
        }

        public void SetDropped(bool bDropped)
        {
            if (bDropped)
                this.BorderBrush = new SolidColorBrush(Colors.Chocolate);
            else
                this.BorderBrush = normalBorderBrush;
        }

        public void OnDropped(IDragable dragable)
        {
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
