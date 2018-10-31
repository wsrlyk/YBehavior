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

        public bool ShowSwitcher
        {
            get { return Switcher.Visibility == Visibility.Visible; }
            set { Switcher.Visibility = value ? Visibility.Visible : Visibility.Collapsed; }
        }
        public UIVariable()
        {
            InitializeComponent();
        }

        private void Remover_Click(object sender, RoutedEventArgs e)
        {
            if (Core.DebugMgr.Instance.IsDebugging())
                return;

            Core.Variable v = DataContext as Core.Variable;
            if (v == null)
                return;

            MessageBoxResult dr = MessageBox.Show("Remove variable " + v.Name + "?", "Remove Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (dr == MessageBoxResult.Yes)
            {

                if (v.SharedDataSource.SharedData != null)
                    (v.SharedDataSource.SharedData as Core.TreeMemory).RemoveVariable(v);
            }
        }

        private void UserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Core.Variable v = e.NewValue as Core.Variable;
            if (v == null)
                return;

            _RefreshVType(v);
        }

        private void VTypes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Core.Variable v = DataContext as Core.Variable;
            if (v == null)
                return;

            _RefreshVType(v);
        }

        void _RefreshVType(Core.Variable v)
        {
            if (v.vType == Core.Variable.ValueType.VT_ENUM)
            {
                this.VConst.Visibility = Visibility.Collapsed;
                this.VBool.Visibility = Visibility.Collapsed;
                this.VEnum.Visibility = Visibility.Visible;
            }
            else if (v.vType == Core.Variable.ValueType.VT_BOOL && v.cType == Core.Variable.CountType.CT_SINGLE)
            {
                this.VConst.Visibility = Visibility.Collapsed;
                this.VBool.Visibility = Visibility.Visible;
                this.VEnum.Visibility = Visibility.Collapsed;
            }
            else
            {
                this.VConst.Visibility = Visibility.Visible;
                this.VBool.Visibility = Visibility.Collapsed;
                this.VEnum.Visibility = Visibility.Collapsed;
            }
        }
    }
}
