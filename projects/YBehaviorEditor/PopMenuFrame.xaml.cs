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
    public class MenuStyleSelector : StyleSelector
    {
        public override Style SelectStyle(object item, DependencyObject container)
        {
            if (item is MenuItemHeadViewModel)
            {
                return ((FrameworkElement)container).FindResource("stMenuHeader") as Style;
            }
            return base.SelectStyle(item, container);
        }
    }
    /// <summary>
    /// PopMenu.xaml 的交互逻辑
    /// </summary>
    public partial class PopMenuFrame : UserControl
    {
        public PopMenuFrame()
        {
            InitializeComponent();
            EventMgr.Instance.Register(EventType.PopMenu, _OnShowPopMenu);
        }

        private void _OnShowPopMenu(EventArg arg)
        {
            PopMenuArg oArg = arg as PopMenuArg;
            MenuItemViewModel model = oArg.MenuModel as MenuItemViewModel;
            this.Menu.ItemsSource = model.MenuItems;
            this.Menu.IsOpen = true;
        }
    }
}
