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
using YBehavior.Editor.Core.New;

namespace YBehavior.Editor
{
    /// <summary>
    /// UI of system tips
    /// </summary>
    public partial class SystemTipsFrame : UserControl
    {
        Storyboard m_InstantAnim;
        Color m_ErrorColor = Color.FromRgb(0xE4, 0x7A, 0x48);
        Color m_SuccessColor = Color.FromRgb(0x48, 0xE4, 0x64);

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
            this.Border.Background = new SolidColorBrush(oArg.TipType == ShowSystemTipsArg.TipsType.TT_Success ? m_SuccessColor : m_ErrorColor);
            m_InstantAnim.Begin(this.Bg, true);

            if (oArg.TipType == ShowSystemTipsArg.TipsType.TT_Success)
                LogMgr.Instance.Log(oArg.Content);
            else
                LogMgr.Instance.Error(oArg.Content);
        }
    }
}
