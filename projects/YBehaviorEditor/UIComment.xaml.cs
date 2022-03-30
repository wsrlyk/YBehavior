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
    /// UIComment.xaml 的交互逻辑
    /// </summary>
    public partial class UIComment : YUserControl, ISelectable, IDeletable
    {
        static SelectionStateChangeHandler defaultSelectHandler = SelectionMgr.Instance.OnSingleSelectedChange;

        public SelectionStateChangeHandler SelectHandler { get; set; }

        //Operation m_Operation;
        Operation m_Resize0Operation;
        Operation m_Resize1Operation;
        Operation m_Move0Operation;
        Operation m_Move1Operation;

        public UIComment()
        {
            InitializeComponent();

            this.selectCover.Visibility = Visibility.Collapsed;

            SelectHandler = defaultSelectHandler;

            m_Move0Operation = new Operation(this.moveBottomLeft);
            m_Move0Operation.RegisterLeftClick(_OnClick);
            m_Move0Operation.RegisterLeftDrag(_OnDrag, null, _OnDragFinish);
            m_Move1Operation = new Operation(this.moveTopRight);
            m_Move1Operation.RegisterLeftClick(_OnClick);
            m_Move1Operation.RegisterLeftDrag(_OnDrag, null, _OnDragFinish);

            m_Resize0Operation = new Operation(this.resizeBottomRight);
            m_Resize0Operation.RegisterLeftDrag(_OnResizeDrag0, null, _OnDragFinish);
            m_Resize1Operation = new Operation(this.resizeTopLeft);
            m_Resize1Operation.RegisterLeftDrag(_OnResizeDrag1, null, _OnDragFinish);
        }

        private void _OnDragFinish(Vector delta, Point pos)
        {
            Comment data = this.DataContext as Comment;
            data.OnFinishGeometryChanged();
        }

        private void _OnResizeDrag0(Vector delta, Point absPos)
        {
            if (NetworkMgr.Instance.IsConnected)
                return;

            Comment data = this.DataContext as Comment;
            data.Geo.BottomRightPos = data.Geo.BottomRightPos + delta;

            data.OnGeometryChanged();
        }

        private void _OnResizeDrag1(Vector delta, Point absPos)
        {
            if (NetworkMgr.Instance.IsConnected)
                return;

            Comment data = this.DataContext as Comment;
            data.Geo.TopLeftPos = data.Geo.TopLeftPos + delta;

            data.OnGeometryChanged();
        }

        void _OnClick(Point pos)
        {
            SelectHandler(this, true);

            m_Move0Operation.MakeCanvasFocused();
        }

        void _OnDrag(Vector delta, Point pos)
        {
            if (NetworkMgr.Instance.IsConnected)
                return;

            Comment data = this.DataContext as Comment;
            data.Geo.Pos = data.Geo.Pos + delta;

            data.OnGeometryChanged();
        }

        public void OnDelete(int param)
        {
            Comment data = this.DataContext as Comment;
            WorkBenchMgr.Instance.RemoveComment(data);
        }

        public void OnDuplicated(int param)
        {
            throw new NotImplementedException();
        }

        public void SetSelect(bool bSelect)
        {
            if (bSelect)
                this.selectCover.Visibility = Visibility.Visible;
            else
                this.selectCover.Visibility = Visibility.Collapsed;
        }
    }
}
