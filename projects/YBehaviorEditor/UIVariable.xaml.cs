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

namespace YBehavior.Editor
{
    /// <summary>
    /// UIVariable.xaml 的交互逻辑
    /// </summary>
    public partial class UIVariable : UserControl
    {
        public string VariableKey
        {
            get { return VKey.Text; }
            set { VKey.Text = value; }
        }

        public string VariableValue
        {
            get { return VValue.Text; }
            set { VValue.Text = value; }
        }

        public bool ShowSwitcher
        {
            get { return Switcher.Visibility == Visibility.Visible; }
            set { Switcher.Visibility = value ? Visibility.Visible : Visibility.Collapsed; }
        }
        public UIVariable()
        {
            InitializeComponent();

        }
    }
}
