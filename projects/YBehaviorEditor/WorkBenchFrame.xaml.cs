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
            EventMgr.Instance.Register(EventType.NewNodeAdded, _OnNewNodeAdded);
            Focus();

            DraggingConnection.Instance.SetCanvas(this.Canvas);
        }

        private void _OnWorkBenchLoaded(EventArg arg)
        {
            WorkBenchLoadedArg oArg = arg as WorkBenchLoadedArg;
            if (oArg.Bench == null)
                return;

            _RenderActiveWorkBench();
        }

        private void _OnNewNodeAdded(EventArg arg)
        {
            NewNodeAddedArg oArg = arg as NewNodeAddedArg;
            if (oArg.Node == null)
                return;

            ///> TODO: move the node to the center of the canvas
            
            _RenderNode(oArg.Node);
        }

        void _RenderActiveWorkBench()
        {
            WorkBench bench = WorkBenchMgr.Instance.ActiveWorkBench;
            _RenderNode(bench.MainTree);

            foreach (var node in bench.Forest)
            {
                _RenderNode(node);
            }

            this.Canvas.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Render, new Action(_ThreadRenderConnections));
        }

        private void _ThreadRenderConnections()
        {
            WorkBench bench = WorkBenchMgr.Instance.ActiveWorkBench;
            bench.MainTree.Renderer.RenderConnections();

            foreach (var node in bench.Forest)
            {
                node.Renderer.RenderConnections();
            }
        }
        void _RenderNode(Node node)
        {
            node.Renderer.Render(this.Canvas);
        }

        private void Operation0_Click(object sender, RoutedEventArgs e)
        {
            _ThreadRenderConnections();
        }

        private void _KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Delete:
                    Core.SelectionMgr.Instance.TryDeleteSelection();
                    break;
            }
        }
    }

}
