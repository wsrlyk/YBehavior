using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using YBehavior.Editor.Core.New;

namespace YBehavior.Editor
{
    /// <summary>
    /// WorkBenchFrame.xaml 的交互逻辑
    /// </summary>
    public partial class TreeBenchFrame : WorkBenchFrame
    {
        public override FrameworkElement GetCanvasBoard { get { return CanvasBoard; } }
        public override FrameworkElement GetCanvas { get { return canvas; } }


        public TreeBenchFrame()
        {
            InitializeComponent();
            _Init();
        }

        public override void OnWorkBenchSelected(EventArg arg)
        {

            base.OnWorkBenchSelected(arg);
            WorkBenchSelectedArg oArg = arg as WorkBenchSelectedArg;

            if (oArg.Bench != null)
            {
                //if (DebugMgr.Instance.IsDebugging(oArg.Bench.FileInfo.Name) && DebugMgr.Instance.bBreaked)
                //{
                //    _RefreshMainTreeDebug(false, NetworkMgr.Instance.MessageProcessor.TickResultToken);
                //}

                oArg.Bench.NodeList.ReAdd();
                oArg.Bench.ConnectionList.ReAdd();
                this.nodeLayer.ItemsSource = oArg.Bench.NodeList.Collection;
                this.commentLayer.ItemsSource = oArg.Bench.Comments;
                this.connectionLayer.ItemsSource = oArg.Bench.ConnectionList.Collection;

                DraggingConnection<UIConnection>.Instance.SetCanvas(this.canvas);
            }
            else
            {
                this.nodeLayer.ItemsSource = null;
                this.commentLayer.ItemsSource = null;
                this.connectionLayer.ItemsSource = null;
            }
        }

        protected override void _OnTickResult(EventArg arg)
        {
            if (DebugMgr.Instance.IsDebugging())
            {
                TickResultArg oArg = arg as TickResultArg;

                _RefreshMainTreeDebug(oArg.Token);
                //this.nodeLayer.Dispatcher.BeginInvoke(new Action<bool, uint>(_RefreshMainTreeDebug), oArg.bInstant, oArg.Token);
            }
        }

        void _RefreshMainTreeDebug(uint token)
        {
            if (token != NetworkMgr.Instance.MessageProcessor.TickResultToken)
            {
                return;
            }

            WorkBench bench = WorkBenchMgr.Instance.ActiveWorkBench;
            if (bench is TreeBench)
                (bench as TreeBench).Tree.Root.Renderer.RefreshDebug();
        }

        protected override void _OnDebugTargetChanged(EventArg arg)
        {
            WorkBench bench = WorkBenchMgr.Instance.ActiveWorkBench;
            if (bench != null)
            {
                if (bench is TreeBench)
                    (bench as TreeBench).Tree.Root.Renderer.RefreshDebug();
            }
        }
    }
}
