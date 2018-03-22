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
    public partial class VariablesFrame : UserControl
    {
        public VariablesFrame()
        {
            InitializeComponent();

            EventMgr.Instance.Register(EventType.SelectionChanged, _OnSelectionChanged);
            EventMgr.Instance.Register(EventType.SharedVariableChanged, _OnSharedVariableChanged);
        }

        private void _OnSelectionChanged(EventArg arg)
        {
            SelectionChangedArg oArg = arg as SelectionChangedArg;
            if (oArg.Target == null)
                return;

            UINode node = oArg.Target as UINode;
            if (node == null)
            {
                // Clear
                this.VariableContainer.ItemsSource = null;
            }
            else
            {
                this.DataContext = node.Node;
                this.VariableContainer.ItemsSource = null;
                this.VariableContainer.ItemsSource = node.Node.Variables.Datas.Values;
            }
        }

        private void _OnSharedVariableChanged(EventArg arg)
        {
            Node node = this.DataContext as Node;
            if (node == null)
                return;

            node.Variables.RefreshVariables();
        }
    }
}
