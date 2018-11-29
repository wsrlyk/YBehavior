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

                if (v is Core.InOutVariable)
                {
                    if (v.SharedDataSource.InOutData != null)
                        v.SharedDataSource.InOutData.RemoveVariable(v);
                }
                else
                {
                    if (v.SharedDataSource.SharedData != null)
                        v.SharedDataSource.SharedData.RemoveVariable(v);
                }
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

        private void CSwitcher_Click(object sender, RoutedEventArgs e)
        {
            Core.Variable v = DataContext as Core.Variable;
            if (v == null)
                return;

            v.cType = v.cType == Core.Variable.CountType.CT_LIST ? Core.Variable.CountType.CT_SINGLE : Core.Variable.CountType.CT_LIST;
        }

        public static readonly DependencyProperty CandidatesResetProperty =
            DependencyProperty.Register("CandidatesReset",
            typeof(bool), typeof(UIVariable), new FrameworkPropertyMetadata(CandidatesReset_PropertyChanged));
        private static void CandidatesReset_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            UIVariable v = (UIVariable)d;
            v.VPointer.SelectedIndex = -1;
        }

        public bool CandidatesReset
        {
            get { return (bool)GetValue(CandidatesResetProperty); }
            set { SetValue(CandidatesResetProperty, value); }
        }
    }
}
