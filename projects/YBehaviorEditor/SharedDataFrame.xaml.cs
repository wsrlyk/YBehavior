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
    /// UI of shared/local variables and input/output pins of tree
    /// </summary>
    public partial class SharedDataFrame : UserControl
    {
        //List<string> m_Types = new List<string>();
        Tree m_CurTree = null;
        public SharedDataFrame()
        {
            InitializeComponent();

            EventMgr.Instance.Register(EventType.WorkBenchSelected, _OnWorkBenchSelected);
            EventMgr.Instance.Register(EventType.SelectSharedDataTab, _OnSelectTab);
        }

        public void Enable()
        {
            EventMgr.Instance.Register(EventType.NetworkConnectionChanged, _OnNetworkConnectionChanged);
            EventMgr.Instance.Register(EventType.DebugTargetChanged, _OnDebugTargetChanged);
            EventMgr.Instance.Register(EventType.SharedVariableChanged, _OnSharedVariableChanged);
        }

        public void Disable()
        {
            EventMgr.Instance.Unregister(EventType.NetworkConnectionChanged, _OnNetworkConnectionChanged);
            EventMgr.Instance.Unregister(EventType.DebugTargetChanged, _OnDebugTargetChanged);
            EventMgr.Instance.Unregister(EventType.SharedVariableChanged, _OnSharedVariableChanged);
        }

        private void _OnWorkBenchSelected(EventArg arg)
        {
            WorkBenchSelectedArg oArg = arg as WorkBenchSelectedArg;
            if (oArg.Bench == null || !(oArg.Bench is TreeBench))
            {
                this.InOutPanel.DataContext = null;
                this.DataContext = null;
                Disable();
                return;
            }
            Enable();
            m_CurTree = oArg.Bench.MainGraph as Tree;
            if (m_CurTree == null)
                return;
            this.InOutPanel.DataContext = m_CurTree.InOutMemory;

            _RefreshDataContext();
        }
        private void _OnSelectTab(EventArg arg)
        {
            SelectSharedDataTabArg oArg = arg as SelectSharedDataTabArg;
            this.TabController.SelectedIndex = oArg.Tab;
        }

        private void _OnNetworkConnectionChanged(EventArg arg)
        {
            NetworkConnectionChangedArg oArg = arg as NetworkConnectionChangedArg;

            this.Dispatcher.BeginInvoke(new Action<NetworkConnectionChangedArg>
                (
                    (NetworkConnectionChangedArg ooArg) =>
                    {
                        _RefreshDataContext();
                    }
                ),

                oArg);

        }

        private void _OnDebugTargetChanged(EventArg arg)
        {
            _RefreshDataContext();
        }

        void _RefreshDataContext()
        {
            if (DebugMgr.Instance.IsDebugging())
                this.DataContext = DebugMgr.Instance.DebugSharedData;
            else
                this.DataContext = m_CurTree?.SharedData;
        }
        private void _OnSharedVariableChanged(EventArg arg)
        {
            if (m_CurTree != null)
                m_CurTree.InOutMemory.RefreshVariables();
        }

        private void AddSharedVariable_Click(object sender, RoutedEventArgs e)
        {
            if (NetworkMgr.Instance.IsConnected)
                return;
            string name = this.NewSharedVariableName.Text;
            bool res = (m_CurTree.SharedData).TryCreateVariable(
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
            if (NetworkMgr.Instance.IsConnected)
                return;
            string name = this.NewLocalVariableName.Text;
            bool res = (m_CurTree.SharedData).TryCreateVariable(
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
            if (NetworkMgr.Instance.IsConnected)
                return;
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
            if (NetworkMgr.Instance.IsConnected)
                return;
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
