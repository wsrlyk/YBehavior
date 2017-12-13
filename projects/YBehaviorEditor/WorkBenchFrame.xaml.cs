using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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
    /// WorkBenchFrame.xaml 的交互逻辑
    /// </summary>
    public partial class WorkBenchFrame : UserControl
    {
        public WorkBenchFrame()
        {
            InitializeComponent();
            EventMgr.Instance.Register(EventType.WorkBenchLoaded, _OnWorkBenchLoaded);
        }

        private void _OnWorkBenchLoaded(EventArg arg)
        {
            WorkBenchLoadedArg oArg = arg as WorkBenchLoadedArg;
            if (oArg.Bench == null)
                return;

            _RenderActiveWorkBench();
        }

        void _RenderActiveWorkBench()
        {
            WorkBench bench = WorkBenchMgr.Instance.ActiveWorkBench;
            _RenderNode(bench.MainTree);

            foreach (var node in bench.Forest)
            {
                _RenderNode(node);
            }
        }

        void _RenderNode(Node node)
        {
            node.Renderer.Render(this.Canvas);
        }
    }

}
