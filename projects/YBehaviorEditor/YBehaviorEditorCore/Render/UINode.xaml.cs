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
        SelectionStateChangeHandler SelectHandler { get; set; }
        Operation m_Operation;

        public UINode()
        {
            InitializeComponent();
            normalBorderBrush = this.border.BorderBrush;

            m_Operation = new Operation(this);
            m_Operation.RegisterClick(_OnClick);
        }

        void _OnClick()
        {
            if (SelectHandler != null)
                SelectHandler(this, true);
            else
                defaultSelectHandler(this, true);
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
