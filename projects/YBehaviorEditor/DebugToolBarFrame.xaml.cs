using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using YBehavior.Editor.Core.New;

namespace YBehavior.Editor
{
    /// <summary>
    /// DebugFrame.xaml 的交互逻辑
    /// </summary>
    public partial class DebugToolBarFrame : UserControl
    {
        public DebugToolBarFrame()
        {
            InitializeComponent();
            this.DebuggingFrame.Visibility = Visibility.Collapsed;
            EventMgr.Instance.Register(EventType.NetworkConnectionChanged, _OnNetworkConnectionChanged);

            this.IP.SetBinding(TextBox.TextProperty, new Binding()
            {
                Path = new PropertyPath("DebugIP"),
                Mode = BindingMode.TwoWay,
                Source = Config.Instance
            });

            this.Port.SetBinding(TextBox.TextProperty, new Binding()
            {
                Path = new PropertyPath("DebugPort"),
                Mode = BindingMode.TwoWay,
                Source = Config.Instance
            });
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
                            this.ConnectFrame.Visibility = Visibility.Collapsed;
                            this.DebuggingFrame.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            this.ConnectFrame.Visibility = Visibility.Visible;
                            this.DebuggingFrame.Visibility = Visibility.Collapsed;
                        }
                    }
                ),
                
                oArg);
        }

        private void btnStartDebug_Click(object sender, RoutedEventArgs e)
        {
            //WorkBench bench = WorkBenchMgr.Instance.ActiveWorkBench;
            //if (bench == null)
            //{
            //    ShowSystemTipsArg arg = new ShowSystemTipsArg()
            //    {
            //        Content = "Should Open A Tree.",
            //        TipType = ShowSystemTipsArg.TipsType.TT_Error,
            //    };
            //    EventMgr.Instance.Send(arg);
            //    return;
            //}
            bool closeUnsavedBench = false;
            foreach (WorkBench openedBench in WorkBenchMgr.Instance.OpenedBenches)
            {
                if (openedBench.CommandMgr.Dirty)
                {
                    MessageBoxResult dr = MessageBox.Show(
                        "Ignore and Close All Unsaved Files?",
                        "Unsaved Files",
                        MessageBoxButton.YesNo, 
                        MessageBoxImage.Question);

                    if (dr == MessageBoxResult.No)
                    {
                        return;
                    }
                    else if (dr == MessageBoxResult.Yes)
                    {
                        closeUnsavedBench = true;
                        break;
                    }
                }
            }
            if (closeUnsavedBench)
            {
                for (int i = WorkBenchMgr.Instance.OpenedBenches.Count - 1; i >= 0; --i)
                {
                    WorkBench openedBench = WorkBenchMgr.Instance.OpenedBenches[i];
                    if (openedBench.CommandMgr.Dirty)
                    {
                        WorkBenchMgr.Instance.Remove(openedBench);
                    }
                }
            }
            //if (bench.CommandMgr.Dirty)
            //{
            //    ShowSystemTipsArg arg = new ShowSystemTipsArg()
            //    {
            //        Content = "Should Save First.",
            //        TipType = ShowSystemTipsArg.TipsType.TT_Error,
            //    };
            //    EventMgr.Instance.Send(arg);
            //    return;
            //}
            string ip = this.IP.Text;
            string port = this.Port.Text;

            NetworkMgr.Instance.Connect(ip, int.Parse(port));
        }

        private void btnStopDebug_Click(object sender, RoutedEventArgs e)
        {
            NetworkMgr.Instance.Disconnect();
        }

        private void btnDebugThisTree_Click(object sender, RoutedEventArgs e)
        {
            //if (WorkBenchMgr.Instance.ActiveWorkBench == null || WorkBenchMgr.Instance.ActiveWorkBench.FileInfo == null)
            //    return;
            ulong uid = 0;
            if (!string.IsNullOrEmpty(this.debugAgentUID.Text) && !ulong.TryParse(this.debugAgentUID.Text, out uid))
            {
                ShowSystemTipsArg arg = new ShowSystemTipsArg()
                {
                    Content = "Failed parsing the ID.",
                    TipType = ShowSystemTipsArg.TipsType.TT_Error,
                };
                EventMgr.Instance.Send(arg);
            }
            else
            {
                DebugMgr.Instance.StartDebugTreeWithAgent(
                uid, this.waitForBegin.IsChecked ?? false);
            }
        }

        private void btnContinue_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.ProcessCommand(Command.DebugContinue);
        }

        private void btnStepInto_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.ProcessCommand(Command.DebugStepIn);
        }

        private void btnStepOver_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.ProcessCommand(Command.DebugStepOver);
        }
    }
}
