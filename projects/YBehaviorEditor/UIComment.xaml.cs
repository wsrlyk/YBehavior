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
    /// UIComment.xaml 的交互逻辑
    /// </summary>
    public partial class UIComment : UserControl, ISelectable, IDeletable, IDuplicatable
    {
        static SelectionStateChangeHandler defaultSelectHandler = SelectionMgr.Instance.OnSingleSelectedChange;

        Brush normalBorderBrush;
        public SelectionStateChangeHandler SelectHandler { get; set; }

        Operation m_Operation;
        Operation m_ResizeOperation;
        Operation m_MoveOperation;

        RenderCanvas m_Canvas = new RenderCanvas();

        public UIComment()
        {
            InitializeComponent();

            normalBorderBrush = this.border.BorderBrush;

            SelectHandler = defaultSelectHandler;

            m_MoveOperation = new Operation(this.move);
            m_MoveOperation.RegisterClick(_OnClick);
            m_MoveOperation.RegisterDrag(_OnDrag);
            m_MoveOperation.SetCanvas(m_Canvas);

            m_ResizeOperation = new Operation(this.resize);
            m_ResizeOperation.RegisterDrag(_OnResizeDrag);
            m_ResizeOperation.SetCanvas(m_Canvas);
        }

        private void _OnResizeDrag(Vector delta, Point absPos)
        {
            if (DebugMgr.Instance.IsDebugging())
                return;

            Comment data = this.DataContext as Comment;
            data.Geo.BottomRightPos = data.Geo.BottomRightPos + delta;

            data.SendProperty("Geo");
        }

        protected override void OnVisualParentChanged(DependencyObject oldParent)
        {
            base.OnVisualParentChanged(oldParent);
            m_Canvas.Panel = VisualTreeHelper.GetParent(this.VisualParent) as Panel;
        }

        void _OnClick()
        {
            SelectHandler(this, true);

            m_Operation.MakeCanvasFocused();
        }

        void _OnDrag(Vector delta, Point pos)
        {
            if (DebugMgr.Instance.IsDebugging())
                return;

            Comment data = this.DataContext as Comment;
            data.Geo.Pos = data.Geo.Pos + delta;

            data.SendProperty("Geo");
        }

        public void OnDelete(int param)
        {
            throw new NotImplementedException();
        }

        public void OnDuplicated(int param)
        {
            throw new NotImplementedException();
        }

        public void SetSelect(bool bSelect)
        {
            if (bSelect)
                this.border.BorderBrush = new SolidColorBrush(Colors.DarkBlue);
            else
                this.border.BorderBrush = normalBorderBrush;
        }
    }
}
