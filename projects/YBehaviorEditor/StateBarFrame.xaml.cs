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
    /// StateBarFrame.xaml 的交互逻辑
    /// </summary>
    public partial class StateBarFrame : UserControl
    {
        public StateBarFrame()
        {
            InitializeComponent();
            //this.log.SetBinding(TextBlock.TextProperty,
            //    new Binding() {
            //        Path = new PropertyPath("LatestTwoLog"),
            //        Source = LogMgr.Instance,
            //        Mode = BindingMode.OneWay
            //    });
        }
    }
}
