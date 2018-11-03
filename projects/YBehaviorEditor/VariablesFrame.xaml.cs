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
                this.InOutTab.Visibility = Visibility.Collapsed;
                this.InOutTab.DataContext = null;
            }
            else
            {
                this.VariableTab.DataContext = node.Node;
                this.VariableContainer.ItemsSource = node.Node.Variables.Datas;

                this.VariableTab.IsSelected = true;

                if (node.Node is Core.SubTreeNode)
                {
                    this.InOutTab.Visibility = Visibility.Visible;
                    this.InOutTab.DataContext = node.Node;
                }
                else
                {
                    this.InOutTab.Visibility = Visibility.Collapsed;
                    this.InOutTab.DataContext = null;
                }
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

            node.NodeMemory.RefreshVariables();
            if (node is SubTreeNode)
            {
                (node as SubTreeNode).InOutMemory.RefreshVariables();
            }
        }

        private void _OnDebugTargetChanged(EventArg arg)
        {
            this.Dispatcher.BeginInvoke(new Action
                (() =>
                {
                    Node node = this.VariableTab.DataContext as Node;
                    if (node == null)
                        return;

                    foreach (VariableHolder v in node.Variables.Datas)
                    {
                        v.Variable.DebugStateChanged();
                    }

                    this.NickName.IsReadOnly = DebugMgr.Instance.IsDebugging();
                    this.Comment.IsReadOnly = DebugMgr.Instance.IsDebugging();
                })
            );
        }

        private void RefreshInOutButton_Click(object sender, RoutedEventArgs e)
        {
            if (DebugMgr.Instance.IsDebugging())
                return;
            SubTreeNode node = this.InOutTab.DataContext as SubTreeNode;
            if (node == null)
                return;

            if (node.ReloadInOut())
            {
                ShowSystemTipsArg showSystemTipsArg = new ShowSystemTipsArg()
                {
                    Content = "SubTree input/output reloaded.",
                    TipType = ShowSystemTipsArg.TipsType.TT_Success,
                };
                EventMgr.Instance.Send(showSystemTipsArg);
            }
        }

        private void TabController_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source is TabControl)
            {
                if (this.InOutTab.IsSelected)
                {
                    if((this.InOutTab.DataContext as Core.SubTreeNode).LoadInOut())
                    {
                        ShowSystemTipsArg showSystemTipsArg = new ShowSystemTipsArg()
                        {
                            Content = "SubTree input/output auto loaded.",
                            TipType = ShowSystemTipsArg.TipsType.TT_Success,
                        };
                        EventMgr.Instance.Send(showSystemTipsArg);
                    }
                }
            }

        }
    }
}
