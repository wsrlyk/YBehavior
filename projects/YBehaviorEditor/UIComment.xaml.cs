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
    public partial class UIComment : YUserControl, ISelectable, IDeletable
    {
        static SelectionStateChangeHandler defaultSelectHandler = SelectionMgr.Instance.OnSingleSelectedChange;

        Brush normalBorderBrush;
        public SelectionStateChangeHandler SelectHandler { get; set; }

        //Operation m_Operation;
        Operation m_Resize0Operation;
        Operation m_Resize1Operation;
        Operation m_Move0Operation;
        Operation m_Move1Operation;

        public UIComment()
        {
            InitializeComponent();

            normalBorderBrush = this.border.BorderBrush;

            SelectHandler = defaultSelectHandler;

            m_Move0Operation = new Operation(this.moveBottomLeft);
            m_Move0Operation.RegisterClick(_OnClick);
            m_Move0Operation.RegisterDrag(_OnDrag);
            m_Move1Operation = new Operation(this.moveTopRight);
            m_Move1Operation.RegisterClick(_OnClick);
            m_Move1Operation.RegisterDrag(_OnDrag);

            m_Resize0Operation = new Operation(this.resizeBottomRight);
            m_Resize0Operation.RegisterDrag(_OnResizeDrag0);
            m_Resize1Operation = new Operation(this.resizeTopLeft);
            m_Resize1Operation.RegisterDrag(_OnResizeDrag1);
        }

        private void _OnResizeDrag0(Vector delta, Point absPos)
        {
            if (DebugMgr.Instance.IsDebugging())
                return;

            Comment data = this.DataContext as Comment;
            data.Geo.BottomRightPos = data.Geo.BottomRightPos + delta;

            data.OnGeometryChanged();
        }

        private void _OnResizeDrag1(Vector delta, Point absPos)
        {
            if (DebugMgr.Instance.IsDebugging())
                return;

            Comment data = this.DataContext as Comment;
            data.Geo.TopLeftPos = data.Geo.TopLeftPos + delta;

            data.OnGeometryChanged();
        }

        void _OnClick()
        {
            SelectHandler(this, true);

            m_Move0Operation.MakeCanvasFocused();
        }

        void _OnDrag(Vector delta, Point pos)
        {
            if (DebugMgr.Instance.IsDebugging())
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
                this.border.BorderBrush = new SolidColorBrush(Colors.DarkBlue);
            else
                this.border.BorderBrush = normalBorderBrush;
        }
    }
}
