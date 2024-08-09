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
    /// UI of variables or pins
    /// </summary>
    public partial class UIVariable : UserControl
    {
        public UIVariable()
        {
            InitializeComponent();
        }

        private void Remover_Click(object sender, RoutedEventArgs e)
        {
            if (NetworkMgr.Instance.IsConnected)
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

            v.CandidatesResetEvent += V_CandidatesResetEvent;
        }

        private void V_CandidatesResetEvent()
        {
            VPointer.SelectedIndex = -1;
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

        private void ContainerSwitcher_Click(object sender, RoutedEventArgs e)
        {
            if (NetworkMgr.Instance.IsConnected)
                return;

            Variable v = DataContext as Variable;
            if (v == null)
                return;

            MessageBoxResult dr = MessageBox.Show("Switch variable " + v.Name + "?", "Switch Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (dr == MessageBoxResult.Yes)
            {
                if (WorkBenchMgr.Instance.ActiveWorkBench is TreeBench)
                {
                    (WorkBenchMgr.Instance.ActiveWorkBench as TreeBench).Switch(v);
                }
            }
        }

        private void VKey_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            Variable v = DataContext as Variable;
            if (v == null)
                return;

            Clipboard.SetText(v.Name);
            ShowSystemTipsArg arg = new ShowSystemTipsArg()
            {
                Content = v.Name + "   ->   Clipboard",
                TipType = ShowSystemTipsArg.TipsType.TT_Success,
            };
            EventMgr.Instance.Send(arg);
        }
    }
}
