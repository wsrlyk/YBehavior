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
    /// BehaviorNode.xaml 的交互逻辑
    /// </summary>
    public partial class UINode : UserControl, ISelectable
    {
        static SelectionStateChangeHandler defaultSelectHandler = new SelectionStateChangeHandler(SelectionMgr.Instance.OnSingleSelectedChange);

        Brush normalBorderBrush;
        public SelectionStateChangeHandler SelectHandler { get; set; }
        
        public Node Node { get; set; }

        Operation m_Operation;

        public UINode()
        {
            InitializeComponent();
            normalBorderBrush = this.border.BorderBrush;
        }

        public void SetCanvas(Panel panel)
        {
            m_Operation = new Operation(this.border, panel);
            m_Operation.RegisterClick(_OnClick);
            m_Operation.RegisterDrag(_OnDrag);
        }


        void _OnClick()
        {
            if (SelectHandler != null)
                SelectHandler(this, true);
            else
                defaultSelectHandler(this, true);
        }

        void _OnDrag(Vector delta, Point pos)
        {
            if (Node != null)
                Node.Renderer.DragMain(delta, pos);
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
