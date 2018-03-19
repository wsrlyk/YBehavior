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
    public partial class SharedDataFrame : UserControl
    {
        public SharedDataFrame()
        {
            InitializeComponent();

            EventMgr.Instance.Register(EventType.WorkBenchLoaded, _OnWorkBenchLoaded);
        }

        private void _OnWorkBenchLoaded(EventArg arg)
        {
            WorkBenchLoadedArg oArg = arg as WorkBenchLoadedArg;
            if (oArg.Bench == null)
                return;

            this.VariableContainer.ItemsSource = oArg.Bench.MainTree.Variables.Datas;
        }
    }
}
