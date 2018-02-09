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

            ///> Check if the bench has already been in the tabs

            UCTabItemWithClose activeTab = null;
            foreach (UCTabItemWithClose tab in this.TabController.Items)
            {
                if (tab.Content == oArg.Bench)
                {
                    activeTab = tab;
                    break;
                }
            }
            ///> Create new tab
            if (activeTab == null)
            {
                activeTab = new UCTabItemWithClose();
                activeTab.Header = oArg.Bench.FileInfo.DisplayName;
                activeTab.ToolTip = oArg.Bench.FileInfo.DisplayPath;
                activeTab.Content = oArg.Bench;
                activeTab.CloseHandler = _TabCloseClicked;
                this.TabController.Items.Add(activeTab);
            }

            activeTab.IsSelected = true;

            //_RenderActiveWorkBench();
        }

        private bool _TabCloseClicked(UCTabItemWithClose tab)
        {
            if (tab == null)
                return false;

            WorkBench bench = tab.Content as WorkBench;
            if (bench == null)
                return true;

            // if is dirty
            {
                MessageBoxResult dr = MessageBox.Show("This file has been modified. Save it?", "To Save Or Not To Save", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                if (dr == MessageBoxResult.Yes)
                {
                    if (WorkBenchMgr.Instance.SaveWorkBench(bench))
                        return true;
                }
                else if (dr == MessageBoxResult.No)
                {
                    WorkBenchMgr.Instance.Remove(bench);
                    return true;
                }
                else
                {
                    return false;
                }
            }

            return true;
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

        
        private void _KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Delete:
                    Core.SelectionMgr.Instance.TryDeleteSelection();
                    break;
            }
        }

        public void ClearCanvas()
        {
            this.Canvas.Children.Clear();
        }
        private void TabController_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source is TabControl)
            {
                ClearCanvas();
                if (e.AddedItems.Count > 0)
                {
                    foreach (UCTabItemWithClose tab in e.AddedItems)
                    {
                        LogMgr.Instance.Log("Tab selected: " + tab.Header);
                        if (WorkBenchMgr.Instance.Switch(tab.Content as WorkBench))
                            _RenderActiveWorkBench();
                        return;
                    }

                    LogMgr.Instance.Error("Tab switch failed.");
                }
            }
        }
    }

}
