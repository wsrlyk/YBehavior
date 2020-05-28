using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using YBehavior.Editor.Core.New;

namespace YBehavior.Editor
{
    public class FSMStateTypeTemplateSelector : DataTemplateSelector
    {
        public DataTemplate NormalTemplate { get; set; }

        public DataTemplate MetaTemplate { get; set; }
        public DataTemplate SpecialTemplate { get; set; }
        public DataTemplate SpecialVirtualTemplate { get; set; }
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            FSMStateNode node = (item as FSMStateRenderer).FSMStateOwner;
            if (node is FSMMetaStateNode)
                return MetaTemplate;
            if (node is FSMNormalStateNode)
                return NormalTemplate;
            if (node is FSMUpperStateNode || node is FSMAnyStateNode)
                return SpecialVirtualTemplate;
            return SpecialTemplate;
        }
    }
    /// <summary>
    /// WorkBenchFrame.xaml 的交互逻辑
    /// </summary>
    public partial class FSMBenchFrame : WorkBenchFrame
    {
        public override FrameworkElement GetCanvasBoard { get { return CanvasBoard; } }
        public override FrameworkElement GetCanvas { get { return canvas; } }


        public FSMBenchFrame()
        {
            InitializeComponent();
            _Init();
        }

        public override void OnWorkBenchLoaded(WorkBench bench)
        {
            base.OnWorkBenchLoaded(bench);

            if (bench != null)
            {
                //if (DebugMgr.Instance.IsDebugging(bench.FileInfo.Name) && DebugMgr.Instance.bBreaked)
                //{
                //    _RefreshMainTreeDebug(false, NetworkMgr.Instance.MessageProcessor.TickResultToken);
                //}

                bench.NodeList.ReAdd();
                bench.ConnectionList.ReAdd();
                this.nodeLayer.ItemsSource = bench.NodeList.Collection;
                this.commentLayer.ItemsSource = bench.Comments;
                this.connectionLayer.ItemsSource = bench.ConnectionList.Collection;
                this.MachineStack.ItemsSource = (bench as FSMBench).StackMachines;

            }
            else
            {
                this.nodeLayer.ItemsSource = null;
                this.commentLayer.ItemsSource = null;
                this.connectionLayer.ItemsSource = null;
                this.MachineStack.ItemsSource = null;
            }
        }

        public override void OnWorkBenchSelected()
        {
            base.OnWorkBenchSelected();
            DraggingConnection<FSMUIConnection>.Instance.SetCanvas(this.canvas);
        }

        private void OnMachineStackItemClicked(object sender, RoutedEventArgs e)
        {
            FSMMachineNode node = (sender as Button).DataContext as FSMMachineNode;
            WorkBenchMgr.Instance.AddRenderers(node, true, false);
        }
    }
}
