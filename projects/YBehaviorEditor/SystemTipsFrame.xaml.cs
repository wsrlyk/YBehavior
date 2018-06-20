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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using YBehavior.Editor.Core;

namespace YBehavior.Editor
{
    /// <summary>
    /// SystemTipsFrame.xaml 的交互逻辑
    /// </summary>
    public partial class SystemTipsFrame : UserControl
    {
        Storyboard m_InstantAnim;
        public SystemTipsFrame()
        {
            InitializeComponent();

            m_InstantAnim = this.Resources["InstantShowAnim"] as Storyboard;
            this.Bg.Visibility = Visibility.Collapsed;
            EventMgr.Instance.Register(EventType.ShowSystemTips, _OnShowSystemTips);
        }

        private void _OnShowSystemTips(EventArg arg)
        {
            ShowSystemTipsArg oArg = arg as ShowSystemTipsArg;
            this.Str.Text = oArg.Content;

            m_InstantAnim.Begin(this.Bg, true);
        }
    }
}
