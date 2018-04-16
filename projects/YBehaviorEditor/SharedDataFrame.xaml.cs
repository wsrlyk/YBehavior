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
    /// SharedDataFrame.xaml 的交互逻辑
    /// </summary>
    public partial class SharedDataFrame : UserControl
    {
        List<string> m_Types = new List<string>();
        Tree m_CurTree = null;
        public SharedDataFrame()
        {
            InitializeComponent();

            EventMgr.Instance.Register(EventType.WorkBenchLoaded, _OnWorkBenchLoaded);
            EventMgr.Instance.Register(EventType.NetworkConnectionChanged, _OnNetworkConnectionChanged);
            EventMgr.Instance.Register(EventType.DebugTargetChanged, _OnDebugTargetChanged);

            foreach (KeyValuePair<Variable.ValueType, string> pair in VariableHelper.ValueTypeDic)
            {
                m_Types.Add(pair.Value);
            }

            this.VType.ItemsSource = m_Types;
        }

        private void _OnWorkBenchLoaded(EventArg arg)
        {
            WorkBenchLoadedArg oArg = arg as WorkBenchLoadedArg;
            if (oArg.Bench == null)
                return;

            m_CurTree = oArg.Bench.MainTree;
            //this.VariableContainer.SetBinding(ComboBox.ItemsSourceProperty, new Binding("Datas"));
            if (DebugMgr.Instance.IsDebugging())
                this.DataContext = DebugMgr.Instance.DebugSharedData;
            else
                this.DataContext = m_CurTree.Variables;
        }

        private void _OnNetworkConnectionChanged(EventArg arg)
        {
            NetworkConnectionChangedArg oArg = arg as NetworkConnectionChangedArg;
            if (oArg.bConnected)
            {
                ///> make Add Button unable to click
            }
        }

        private void _OnDebugTargetChanged(EventArg arg)
        {
            this.DataContext = DebugMgr.Instance.DebugSharedData;
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            if (m_CurTree == null)
            {
                LogMgr.Instance.Error("There's no active tree.");
                return;
            }
            string name = this.VName.Text;
            string value = this.VValue.Text;
            string type = this.VType.SelectedValue as string;
            int isarray = this.VIsArray.SelectedIndex;

            if (m_CurTree.Variables.TryCreateVariable(
                name, 
                value, 
                VariableHelper.ValueTypeDic.GetKey(type, Variable.ValueType.VT_NONE),
                isarray == 1 ? Variable.CountType.CT_LIST : Variable.CountType.CT_SINGLE))
            {
            }
            else
            {
                LogMgr.Instance.Error("Variable creation failed. Check the params.");
                return;
            }

        }
    }
}
