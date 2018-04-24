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
    class PageData
    {
        public TransformGroup TransGroup { get; } = new TransformGroup();

        public ScaleTransform ScaleTransform { get; } = new ScaleTransform(1.0f, 1.0f);
        public TranslateTransform TranslateTransform { get; } = new TranslateTransform();

        public PageData()
        {
            TransGroup.Children.Add(ScaleTransform);
            TransGroup.Children.Add(TranslateTransform);
        }
    }
    /// <summary>
    /// WorkBenchFrame.xaml 的交互逻辑
    /// </summary>
    public partial class WorkBenchFrame : UserControl
    {
        Dictionary<UCTabItemWithClose, PageData> m_PageDataDic = new Dictionary<UCTabItemWithClose, PageData>();

        PageData m_CurPageData;
        Core.Operation m_Operation;

        public WorkBenchFrame()
        {
            InitializeComponent();
            EventMgr.Instance.Register(EventType.WorkBenchLoaded, _OnWorkBenchLoaded);
            EventMgr.Instance.Register(EventType.WorkBenchSelected, _OnWorkBenchSelected);
            EventMgr.Instance.Register(EventType.NewNodeAdded, _OnNewNodeAdded);
            EventMgr.Instance.Register(EventType.TickResult, _OnTickResult);
            EventMgr.Instance.Register(EventType.NetworkConnectionChanged, _OnNetworkConnectionChanged);
            EventMgr.Instance.Register(EventType.DebugTargetChanged, _OnDebugTargetChanged);
            Focus();

            DraggingConnection.Instance.SetCanvas(this.Canvas);

            m_Operation = new Operation(this.CanvasBoard);
            m_Operation.RegisterDrag(_OnDrag);
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

                m_PageDataDic[activeTab] = new PageData();
            }

            activeTab.IsSelected = true;

            //_RenderActiveWorkBench();
        }

        private void _OnWorkBenchSelected(EventArg arg)
        {
            ClearCanvas();
            _CreateActiveWorkBench();
        }

        private void _OnTickResult(EventArg arg)
        {
            if (DebugMgr.Instance.IsDebugging())
            {
                TickResultArg oArg = arg as TickResultArg;

                this.Canvas.Dispatcher.BeginInvoke(new Action<bool, uint>(_RefreshMainTreeDebug), oArg.bInstant, oArg.Token);
            }
        }

        void _RefreshMainTreeDebug(bool bInstant, uint token)
        {
            if (token != NetworkMgr.Instance.MessageProcessor.TickResultToken)
            {
                return;
            }

            WorkBench bench = WorkBenchMgr.Instance.ActiveWorkBench;
            bench.MainTree.Renderer.RefreshDebug(bInstant);
        }

        private void _OnNetworkConnectionChanged(EventArg arg)
        {
            WorkBench bench = WorkBenchMgr.Instance.ActiveWorkBench;
            if (bench != null)
                Dispatcher.BeginInvoke(new Action(() => { bench.MainTree.Renderer.RefreshDebug(true); }));
            
        }

        private void _OnDebugTargetChanged(EventArg arg)
        {
            WorkBench bench = WorkBenchMgr.Instance.ActiveWorkBench;
            if (bench != null)
                Dispatcher.BeginInvoke(new Action(() => { bench.MainTree.Renderer.RefreshDebug(true); }));
        }

        private bool _TabCloseClicked(UCTabItemWithClose tab)
        {
            if (tab == null)
                return false;

            WorkBench bench = tab.Content as WorkBench;
            if (bench != null)
            // if is dirty
            {
                MessageBoxResult dr = MessageBox.Show("This file has been modified. Save it?", "To Save Or Not To Save", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                if (dr == MessageBoxResult.Yes)
                {
                    int res = WorkBenchMgr.Instance.SaveWorkBench(bench);
                    if (res < 0)
                    {
                        MessageBox.Show("Save Failed.");
                        return false;
                    }
                }
                else if (dr == MessageBoxResult.No)
                {
                    WorkBenchMgr.Instance.Remove(bench);
                }
                else
                {
                    return false;
                }
            }

            m_PageDataDic.Remove(tab);

            if (m_PageDataDic.Count == 0)
            {
                WorkBenchSelectedArg arg = new WorkBenchSelectedArg()
                {
                    Bench = null
                };
                EventMgr.Instance.Send(arg);
            }
            return true;
        }

        private void _OnNewNodeAdded(EventArg arg)
        {
            NewNodeAddedArg oArg = arg as NewNodeAddedArg;
            if (oArg.Node == null)
                return;

            ///> move the node to the topleft of the canvas
            oArg.Node.Renderer.SetPos(new Point(
                -m_CurPageData.TranslateTransform.X / m_CurPageData.ScaleTransform.ScaleX, 
                -m_CurPageData.TranslateTransform.Y / m_CurPageData.ScaleTransform.ScaleY));

            _CreateNode(oArg.Node);
            //this.Canvas.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Render, new Action<Node>(_ThreadRefreshConnection), oArg.Node);
        }

        void _CreateActiveWorkBench()
        {
            WorkBench bench = WorkBenchMgr.Instance.ActiveWorkBench;
            if (bench == null)
                return;
            _CreateNode(bench.MainTree);

            foreach (var node in bench.Forest)
            {
                _CreateNode(node);
            }

            //this.Canvas.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Render, new Action(_ThreadRefreshConnections));
        }

        void _CreateNode(Node node)
        {
            node.Renderer.AddedToPanel(this.Canvas);
            //node.Renderer.CreateConnections();
        }


        private void _KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Delete:
                    if ((Keyboard.Modifiers & ModifierKeys.Shift) != ModifierKeys.None)
                    {
                        ///> Duplicate all children
                        Core.SelectionMgr.Instance.TryDeleteSelection(1);
                    }
                    else
                    {
                        ///> Duplicate only one
                        Core.SelectionMgr.Instance.TryDeleteSelection(0);
                    }
                    break;
                case Key.D:
                    if ((Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.None)
                    {
                        if((Keyboard.Modifiers & ModifierKeys.Shift)!= ModifierKeys.None)
                        {
                            ///> Duplicate all children
                            Core.SelectionMgr.Instance.TryDuplicateSelection(1);
                        }
                        else
                        {
                            ///> Duplicate only one
                            Core.SelectionMgr.Instance.TryDuplicateSelection(0);
                        }
                    }
                    break;
                case Key.F9:
                    Core.SelectionMgr.Instance.TryToggleBreakPoint();
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
                if (e.AddedItems.Count > 0)
                {
                    foreach (UCTabItemWithClose tab in e.AddedItems)
                    {
                        LogMgr.Instance.Log("Tab selected: " + tab.Header);
                        if (WorkBenchMgr.Instance.Switch(tab.Content as WorkBench))
                        {
                            m_CurPageData = m_PageDataDic[tab];
                            this.Canvas.RenderTransform = m_CurPageData.TransGroup;
                            return;
                        }
                    }

                    LogMgr.Instance.Error("Tab switch failed.");
                }
            }
        }

        private void _MouseWheel(object sender, MouseWheelEventArgs e)
        {
            Point pos = e.GetPosition(this.CanvasBoard);
            Point oldPos = new Point(m_CurPageData.TranslateTransform.X, m_CurPageData.TranslateTransform.Y);

            double width = this.Canvas.ActualWidth;
            double height = this.Canvas.ActualHeight;

            double oldWidth = width * m_CurPageData.ScaleTransform.ScaleX;
            double oldHeight = height * m_CurPageData.ScaleTransform.ScaleY;

            double rateX = (pos.X - oldPos.X) / oldWidth;
            double rateY = (pos.Y - oldPos.Y) / oldHeight;

            double delta = (e.Delta / Math.Abs(e.Delta) * 0.1);
            m_CurPageData.ScaleTransform.ScaleX *= (1.0 + delta);
            m_CurPageData.ScaleTransform.ScaleY *= (1.0 + delta);

            double deltaX = (width * m_CurPageData.ScaleTransform.ScaleX - oldWidth) * rateX;
            double deltaY = (height * m_CurPageData.ScaleTransform.ScaleY - oldHeight) * rateY;

            m_CurPageData.TranslateTransform.X -= deltaX;
            m_CurPageData.TranslateTransform.Y -= deltaY;
        }


        void _OnDrag(Vector delta, Point pos)
        {
            m_CurPageData.TranslateTransform.X += delta.X;
            m_CurPageData.TranslateTransform.Y += delta.Y;
        }

        public void ResetTransform()
        {
            m_CurPageData.ScaleTransform.ScaleX = 0;
            m_CurPageData.ScaleTransform.ScaleY = 0;
            m_CurPageData.TranslateTransform.X = 0;
            m_CurPageData.TranslateTransform.Y = 0;
        }
    }
}
