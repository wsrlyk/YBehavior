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
        }

        private void _OnNetworkConnectionChanged(EventArg arg)
        {
            NetworkConnectionChangedArg oArg = arg as NetworkConnectionChangedArg;
            if (oArg.bConnected)
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

        private void btnStartDebug_Click(object sender, RoutedEventArgs e)
        {
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

            uint.TryParse(this.debugAgentUID.Text, out uint uid);
            DebugMgr.Instance.StartDebugTreeWithAgent(WorkBenchMgr.Instance.ActiveWorkBench.FileInfo.Name, uid);
        }

        private void btnContinue_Click(object sender, RoutedEventArgs e)
        {
            DebugMgr.Instance.Continue();
        }
    }
}
