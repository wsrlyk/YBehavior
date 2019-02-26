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
        //List<string> m_Types = new List<string>();
        Tree m_CurTree = null;
        public SharedDataFrame()
        {
            InitializeComponent();

            EventMgr.Instance.Register(EventType.WorkBenchSelected, _OnWorkBenchSelected);
            EventMgr.Instance.Register(EventType.NetworkConnectionChanged, _OnNetworkConnectionChanged);
            EventMgr.Instance.Register(EventType.DebugTargetChanged, _OnDebugTargetChanged);
            EventMgr.Instance.Register(EventType.SharedVariableChanged, _OnSharedVariableChanged);

            //foreach (var v in Variable.CreateParams_AllTypes)
            //{
            //    m_Types.Add(Variable.ValueTypeDic2.GetValue(v, ""));
            //}

            //this.VType.ItemsSource = m_Types;
        }

        private void _OnWorkBenchSelected(EventArg arg)
        {
            WorkBenchSelectedArg oArg = arg as WorkBenchSelectedArg;
            if (oArg.Bench == null)
            {
                this.InOutPanel.DataContext = null;
                this.DataContext = null;
                return;
            }
            m_CurTree = oArg.Bench.MainTree;
            this.InOutPanel.DataContext = m_CurTree.InOutMemory;

            if (DebugMgr.Instance.IsDebugging())
                this.DataContext = DebugMgr.Instance.DebugSharedData;
            else
                this.DataContext = m_CurTree.Variables;
        }

        private void _OnNetworkConnectionChanged(EventArg arg)
        {
            NetworkConnectionChangedArg oArg = arg as NetworkConnectionChangedArg;

            this.Dispatcher.BeginInvoke(new Action<NetworkConnectionChangedArg>
                (
                    (NetworkConnectionChangedArg ooArg) =>
                    {
                        if (ooArg.bConnected)
                        {
                            this.DataContext = DebugMgr.Instance.DebugSharedData;
                        }
                        else
                        {
                            this.DataContext = m_CurTree == null ? null : m_CurTree.Variables;
                        }
                    }
                ),

                oArg);

        }

        private void _OnDebugTargetChanged(EventArg arg)
        {
            this.DataContext = DebugMgr.Instance.DebugSharedData;
        }

        private void _OnSharedVariableChanged(EventArg arg)
        {
            if (m_CurTree != null)
                m_CurTree.InOutMemory.RefreshVariables();
        }

        //private void Add_Click(object sender, RoutedEventArgs e)
        //{
        //    if (m_CurTree == null)
        //    {
        //        LogMgr.Instance.Error("There's no active tree.");
        //        return;
        //    }

        //    if (DebugMgr.Instance.IsDebugging())
        //        return;

        //    string name = this.VName.Text;
        //    string value = this.VValue.Text;
        //    string type = this.VType.SelectedValue as string;
        //    bool isarray = this.VIsArray.IsChecked ?? false;
        //    bool islocal = this.VIsLocal.IsChecked ?? false;
        //    bool isinput = this.VIsInput.IsChecked ?? false;

        //    bool isdata = this.IsDatasSelected.IsChecked ?? false;
        //    bool isinout = this.IsInOutSelected.IsChecked ?? false;

        //    Variable.ValueType vtype = Variable.ValueTypeDic2.GetKey(type, Variable.ValueType.VT_NONE);
        //    Variable.CountType ctype = isarray ? Variable.CountType.CT_LIST : Variable.CountType.CT_SINGLE;
        //    if (value == "" && ctype == Variable.CountType.CT_SINGLE)
        //    {
        //        Variable.DefaultValueDic.TryGetValue(vtype, out value);
        //    }

        //    bool res = false;
        //    if (isdata)
        //    {
        //        res = (m_CurTree.Variables as Core.TreeMemory).TryCreateVariable(
        //            name,
        //            value,
        //            vtype,
        //            ctype,
        //            islocal);
        //    }
        //    else if (isinout)
        //    {
        //        res = m_CurTree.InOutMemory.TryCreateVariable(
        //            name,
        //            vtype,
        //            ctype,
        //            isinput);
        //    }
        //}

        private void AddSharedVariable_Click(object sender, RoutedEventArgs e)
        {
            string name = this.NewSharedVariableName.Text;
            bool res = (m_CurTree.Variables as Core.TreeMemory).TryCreateVariable(
                name,
                "0",
                Variable.ValueType.VT_INT,
                Variable.CountType.CT_NONE,
                false);
            if (res)
                this.NewSharedVariableName.Text = string.Empty;
            _OnAddVariable(res);
        }

        private void AddLocalVariable_Click(object sender, RoutedEventArgs e)
        {
            string name = this.NewLocalVariableName.Text;
            bool res = (m_CurTree.Variables as Core.TreeMemory).TryCreateVariable(
                name,
                "0",
                Variable.ValueType.VT_INT,
                Variable.CountType.CT_NONE,
                true);
            if (res)
                this.NewLocalVariableName.Text = string.Empty;
            _OnAddVariable(res);
        }

        private void AddInput_Click(object sender, RoutedEventArgs e)
        {
            string name = this.NewInputName.Text;
            bool res = m_CurTree.InOutMemory.TryCreateVariable(
                name,
                Variable.ValueType.VT_INT,
                Variable.CountType.CT_NONE,
                true);
            if (res)
                this.NewInputName.Text = string.Empty;
            _OnAddVariable(res);
        }

        private void AddOutput_Click(object sender, RoutedEventArgs e)
        {
            string name = this.NewOutputName.Text;
            bool res = m_CurTree.InOutMemory.TryCreateVariable(
                name,
                Variable.ValueType.VT_INT,
                Variable.CountType.CT_NONE,
                false);
            if (res)
                this.NewOutputName.Text = string.Empty;
            _OnAddVariable(res);
        }

        private void _OnAddVariable(bool res)
        {
            if (res)
            {
                ShowSystemTipsArg showSystemTipsArg = new ShowSystemTipsArg()
                {
                    Content = "Created successfully.",
                    TipType = ShowSystemTipsArg.TipsType.TT_Success,
                };
                EventMgr.Instance.Send(showSystemTipsArg);
            }
            else
            {
                ShowSystemTipsArg showSystemTipsArg = new ShowSystemTipsArg()
                {
                    Content = "Variable creation failed. Check the params.",
                    TipType = ShowSystemTipsArg.TipsType.TT_Error,
                };
                EventMgr.Instance.Send(showSystemTipsArg);
                return;
            }

        }
    }
}
