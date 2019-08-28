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
using YBehavior.Editor.Core.New;

namespace YBehavior.Editor
{
    /// <summary>
    /// FSMSelectionFrame.xaml 的交互逻辑
    /// </summary>
    public partial class FSMSelectionFrame : UserControl
    {
        public FSMSelectionFrame()
        {
            InitializeComponent();
            EventMgr.Instance.Register(EventType.WorkBenchSelected, _OnWorkBenchSelected);
        }

        public void Enable()
        {
            EventMgr.Instance.Register(EventType.SelectionChanged, _OnSelectionChanged);

        }

        public void Disable()
        {
            EventMgr.Instance.Unregister(EventType.SelectionChanged, _OnSelectionChanged);
        }

        private void _OnWorkBenchSelected(EventArg arg)
        {
            WorkBenchSelectedArg oArg = arg as WorkBenchSelectedArg;
            if (oArg.Bench == null || !(oArg.Bench is FSMBench))
            {
                Disable();
            }
            else
            {
                Enable();
            }
        }

        private void _OnSelectionChanged(EventArg arg)
        {
            SelectionChangedArg oArg = arg as SelectionChangedArg;
            //if (oArg.Target == null)
            //    return;

            if (oArg.Target is UIFSMState)
            {
                UIFSMState node = oArg.Target as UIFSMState;
                this.StateTab.DataContext = node.DataContext;

                Dispatcher.BeginInvoke((Action)(() => this.TabController.SelectedItem = this.StateTab));
            }
            else if (oArg.Target is UIComment)
            {
                UIComment com = oArg.Target as UIComment;
                this.CommentTab.DataContext = com.DataContext;

                Dispatcher.BeginInvoke((Action)(() => this.TabController.SelectedItem = this.CommentTab));
            }
            else if (oArg.Target is FSMUIConnection)
            {
                FSMUIConnection conn = oArg.Target as FSMUIConnection;
                this.ConnectionTab.DataContext = conn.DataContext;

                Dispatcher.BeginInvoke((Action)(() => this.TabController.SelectedItem = this.ConnectionTab));
            }
        }

        private void TabController_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}
