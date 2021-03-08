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
            if (DebugMgr.Instance.IsDebugging())
                return;

            Variable v = DataContext as Variable;
            if (v == null)
                return;

            MessageBoxResult dr = MessageBox.Show("Remove variable " + v.Name + "?", "Remove Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (dr == MessageBoxResult.Yes)
            {

                if (v is InOutVariable)
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
            Variable v = e.NewValue as Variable;
            if (v == null)
                return;

            _RefreshVType(v);
        }

        private void VTypes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Variable v = DataContext as Variable;
            if (v == null)
                return;

            _RefreshVType(v);
        }

        void _RefreshVType(Variable v)
        {
            if (v.vType == Variable.ValueType.VT_ENUM)
            {
                this.VConst.Visibility = Visibility.Collapsed;
                this.VBool.Visibility = Visibility.Collapsed;
                this.VEnum.Visibility = Visibility.Visible;
            }
            else if (v.vType == Variable.ValueType.VT_BOOL && v.cType == Variable.CountType.CT_SINGLE)
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
            Variable v = DataContext as Variable;
            if (v == null)
                return;

            v.cType = v.cType == Variable.CountType.CT_LIST ? Variable.CountType.CT_SINGLE : Variable.CountType.CT_LIST;
            _RefreshVType(v);
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

        private void VKey_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Variable v = DataContext as Variable;
            if (v == null)
                return;
            EventMgr.Instance.Send(new VariableClickedArg()
            {
                v = v,
            });
        }
    }
}
