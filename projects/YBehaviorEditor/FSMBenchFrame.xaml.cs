﻿using System;
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
                this.MachineStack.ItemsSource = (oArg.Bench as FSMBench).StackMachines;

                DraggingConnection<FSMUIConnection>.Instance.SetCanvas(this.canvas);
            }
            else
            {
                this.nodeLayer.ItemsSource = null;
                this.commentLayer.ItemsSource = null;
                this.connectionLayer.ItemsSource = null;
                this.MachineStack.ItemsSource = null;
            }
        }

        private void OnMachineStackItemClicked(object sender, RoutedEventArgs e)
        {
            FSMMachineNode node = (sender as Button).DataContext as FSMMachineNode;
            WorkBenchMgr.Instance.AddRenderers(node, true, false);
        }
    }
}