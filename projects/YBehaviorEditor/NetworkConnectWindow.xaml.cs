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
using System.Windows.Shapes;

namespace YBehavior.Editor
{
    /// <summary>
    /// NetworkConnect.xaml 的交互逻辑
    /// </summary>
    public partial class NetworkConnectWindow : Window
    {
        public NetworkConnectWindow()
        {
            InitializeComponent();
        }

        private void BtnOK_Click(object sender, RoutedEventArgs e)
        {
            string ip = this.IP.Text;
            string port = this.Port.Text;

            if (Core.NetworkMgr.Instance.Connect(ip, int.Parse(port)))
                this.Close();
        }
    }
}
