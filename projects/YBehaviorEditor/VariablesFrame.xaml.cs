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

            EventMgr.Instance.Register(EventType.WorkBenchSelected, _OnWorkBenchSelected);
            EventMgr.Instance.Register(EventType.SelectionChanged, _OnSelectionChanged);
            EventMgr.Instance.Register(EventType.SharedVariableChanged, _OnSharedVariableChanged);
            EventMgr.Instance.Register(EventType.DebugTargetChanged, _OnDebugTargetChanged);
            EventMgr.Instance.Register(EventType.NetworkConnectionChanged, _OnDebugTargetChanged);
        }

        private void _OnWorkBenchSelected(EventArg arg)
        {
            this.DataContext = null;
            this.VariableContainer.ItemsSource = null;
        }
        private void _OnSelectionChanged(EventArg arg)
        {
            SelectionChangedArg oArg = arg as SelectionChangedArg;
            //if (oArg.Target == null)
            //    return;

            UINode node = oArg.Target as UINode;
            if (node == null)
            {
                // Clear
                this.VariableContainer.ItemsSource = null;
                this.VariableTab.DataContext = null;
            }
            else
            {
                this.VariableTab.DataContext = node.Node;
                this.VariableContainer.ItemsSource = node.Node.Variables.Datas.Values;

                this.VariableTab.IsSelected = true;
            }

            UIComment comment = oArg.Target as UIComment;
            if (comment == null)
            {
                // Clear
                this.CommentTab.DataContext = null;
            }
            else
            {
                this.CommentTab.DataContext = comment.DataContext;

                this.CommentTab.IsSelected = true;

            }
        }

        private void _OnSharedVariableChanged(EventArg arg)
        {
            Node node = this.VariableTab.DataContext as Node;
            if (node == null)
                return;

            node.Variables.RefreshVariables();
        }

        private void _OnDebugTargetChanged(EventArg arg)
        {
            this.Dispatcher.BeginInvoke(new Action
                (() =>
                {
                    Node node = this.VariableTab.DataContext as Node;
                    if (node == null)
                        return;

                    foreach (Variable v in node.Variables.Datas.Values)
                    {
                        v.DebugStateChanged();
                    }

                    this.NickName.IsReadOnly = DebugMgr.Instance.IsDebugging();
                    this.Comment.IsReadOnly = DebugMgr.Instance.IsDebugging();
                })
            );
        }
    }
}
