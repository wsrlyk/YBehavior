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
    /// DebugFrame.xaml 的交互逻辑
    /// </summary>
    public partial class DebugFrame : UserControl
    {
        public DebugFrame()
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
            WorkBench bench = WorkBenchMgr.Instance.ActiveWorkBench;
            if (bench == null)
            {
                ShowSystemTipsArg arg = new ShowSystemTipsArg()
                {
                    Content = "Should Open A Tree.",
                    TipType = ShowSystemTipsArg.TipsType.TT_Error,
                };
                EventMgr.Instance.Send(arg);
                return;
            }
            foreach (WorkBench openedBench in WorkBenchMgr.Instance.OpenedBenches)
            {
                if (openedBench.CommandMgr.Dirty)
                {
                    ShowSystemTipsArg arg = new ShowSystemTipsArg()
                    {
                        Content = "Should Save All Opened Files First.",
                        TipType = ShowSystemTipsArg.TipsType.TT_Error,
                    };
                    EventMgr.Instance.Send(arg);
                    return;
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

            Core.NetworkMgr.Instance.Connect(ip, int.Parse(port));
        }

        private void btnStopDebug_Click(object sender, RoutedEventArgs e)
        {
            NetworkMgr.Instance.Disconnect();
        }

        private void btnDebugThisTree_Click(object sender, RoutedEventArgs e)
        {
            if (WorkBenchMgr.Instance.ActiveWorkBench == null || WorkBenchMgr.Instance.ActiveWorkBench.FileInfo == null)
                return;

            if (!uint.TryParse(this.debugAgentUID.Text, out uint uid))
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
                uid);
            }
        }

        private void btnContinue_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.ProcessKeyDown(Key.F5, ModifierKeys.None);
        }

        private void btnStepInto_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.ProcessKeyDown(Key.F11, ModifierKeys.None);
        }

        private void btnStepOver_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.ProcessKeyDown(Key.F10, ModifierKeys.None);
        }
    }
}
