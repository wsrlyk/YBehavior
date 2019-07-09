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
        Dictionary<TabItem, PageData> m_PageDataDic = new Dictionary<TabItem, PageData>();

        PageData m_CurPageData;
        Core.Operation m_Operation;

        public WorkBenchFrame()
        {
            InitializeComponent();
            EventMgr.Instance.Register(EventType.WorkBenchLoaded, _OnWorkBenchLoaded);
            EventMgr.Instance.Register(EventType.WorkBenchSaved, _OnWorkBenchSaved);
            EventMgr.Instance.Register(EventType.WorkBenchSelected, _OnWorkBenchSelected);
            EventMgr.Instance.Register(EventType.SelectWorkBench, _OnSelectWorkBench);
            EventMgr.Instance.Register(EventType.NewNodeAdded, _OnNewNodeAdded);
            EventMgr.Instance.Register(EventType.TickResult, _OnTickResult);
            EventMgr.Instance.Register(EventType.NetworkConnectionChanged, _OnDebugTargetChanged);
            EventMgr.Instance.Register(EventType.DebugTargetChanged, _OnDebugTargetChanged);
            EventMgr.Instance.Register(EventType.CommentCreated, _OnCommentCreated);
            EventMgr.Instance.Register(EventType.MakeCenter, _OnMakeCenter);
            Focus();

            DraggingConnection.Instance.SetCanvas(this.canvas);

            m_Operation = new Operation(this.CanvasBoard);
            m_Operation.RegisterMiddleDrag(_OnDrag, null, null);
            m_Operation.RegisterLeftClick(_OnClick);
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
                //activeTab.Header = oArg.Bench.FileInfo.DisplayName;
                activeTab.ToolTip = oArg.Bench.FileInfo.DisplayPath;
                activeTab.Content = oArg.Bench;
                activeTab.CloseHandler = _TabCloseClicked;
                this.TabController.Items.Add(activeTab);
                activeTab.PreviewMouseMove += TabItem_PreviewMouseMove;
                activeTab.PreviewMouseLeftButtonDown += TabItem_PreviewMouseLeftButtonDown;
                activeTab.Drop += TabItem_Drop;
                activeTab.AllowDrop = true;
                m_PageDataDic[activeTab] = new PageData();

                activeTab.DataContext = oArg.Bench;
                activeTab.SetBinding(UCTabItemWithClose.HeaderProperty, new Binding()
                {
                    Path = new PropertyPath("DisplayName"),
                    Mode = BindingMode.OneWay
                });
            }

            activeTab.IsSelected = WorkBenchMgr.Instance.ActiveWorkBench == oArg.Bench;

            //_RenderActiveWorkBench();
        }

        private void _OnWorkBenchSaved(EventArg arg)
        {
            WorkBenchSavedArg oArg = arg as WorkBenchSavedArg;
            WorkBench bench = oArg.Bench;
            if (bench == null)
                return;
            ///> Rename the tab title
            if (oArg.bCreate)
            {
                UCTabItemWithClose activeTab = null;
                foreach (UCTabItemWithClose tab in this.TabController.Items)
                {
                    if (tab.Content == bench)
                    {
                        activeTab = tab;
                        break;
                    }
                }
                ///> Create new tab
                if (activeTab != null)
                {
                    activeTab.Header = bench.FileInfo.DisplayName;
                }
            }
        }

        private void _OnWorkBenchSelected(EventArg arg)
        {
            ClearCanvas();
            //_CreateActiveWorkBench();

            WorkBenchSelectedArg oArg = arg as WorkBenchSelectedArg;

            if (oArg.Bench != null)
            {
                //if (DebugMgr.Instance.IsDebugging(oArg.Bench.FileInfo.Name) && DebugMgr.Instance.bBreaked)
                //{
                //    _RefreshMainTreeDebug(false, NetworkMgr.Instance.MessageProcessor.TickResultToken);
                //}

                this.nodeLayer.ItemsSource = oArg.Bench.NodeList;
                this.commentLayer.ItemsSource = oArg.Bench.Comments;
                this.connectionLayer.ItemsSource = oArg.Bench.ConnectionList;
            }
            else
            {
                this.nodeLayer.ItemsSource = null;
                this.commentLayer.ItemsSource = null;
                this.connectionLayer.ItemsSource = null;
            }
        }

        private void _OnSelectWorkBench(EventArg arg)
        {
            SelectWorkBenchArg oArg = arg as SelectWorkBenchArg;

            foreach (UCTabItemWithClose tab in this.TabController.Items)
            {
                if (tab.Content == oArg.Bench)
                {
                    tab.IsSelected = true;
                    break;
                }
            }
        }

        private void _OnTickResult(EventArg arg)
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
            bench.MainTree.Renderer.RefreshDebug();
        }

        private void _OnDebugTargetChanged(EventArg arg)
        {
            WorkBench bench = WorkBenchMgr.Instance.ActiveWorkBench;
            if (bench != null)
            {
                bench.MainTree.Renderer.RefreshDebug();

                //this.Dispatcher.BeginInvoke(new Action
                //    (() =>
                //    {
                //        this.TabController.IsEnabled = !DebugMgr.Instance.IsDebugging();
                //    })
                //);
            }
        }

        private bool _TabCloseClicked(UCTabItemWithClose tab)
        {
            if (tab == null)
                return false;

            WorkBench bench = tab.Content as WorkBench;
            if (bench != null)
            {
                if (DebugMgr.Instance.IsDebugging(bench.FileInfo.Name))
                    return false;

                if (bench.CommandMgr.Dirty)
                // if is dirty
                {
                    MessageBoxResult dr = MessageBox.Show("This file has been modified. Save it?", "To Save Or Not To Save", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                    if (dr == MessageBoxResult.Yes)
                    {
                        int res = WorkBenchMgr.Instance.SaveAndExport(bench);
                        if ((res & WorkBenchMgr.SaveResultFlag_Saved) == 0)
                        {
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

        Point _startPoint;
        void TabItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _startPoint = e.GetPosition(null);
        }

        private void TabItem_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed/* && !IsDragging*/)
            {
                var tabItem = e.Source as UCTabItemWithClose;
                if (tabItem == null)
                    return;

                Point position = e.GetPosition(null);

                if (Math.Abs(position.X - _startPoint.X) > SystemParameters.MinimumHorizontalDragDistance ||
                        Math.Abs(position.Y - _startPoint.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    DragDrop.DoDragDrop(tabItem, tabItem, DragDropEffects.All);
                }
            }
        }

        private void TabItem_Drop(object sender, DragEventArgs e)
        {
            var tabItemTarget = e.Source as TabItem;

            var tabItemSource = e.Data.GetData(typeof(UCTabItemWithClose)) as UCTabItemWithClose;

            if (!tabItemTarget.Equals(tabItemSource))
            {
                var tabControl = tabItemTarget.Parent as TabControl;
                int targetIndex = tabControl.Items.IndexOf(tabItemTarget);

                tabControl.Items.Remove(tabItemSource);
                tabControl.Items.Insert(targetIndex, tabItemSource);

                //tabControl.Items.Remove(tabItemTarget);
                //tabControl.Items.Insert(sourceIndex, tabItemTarget);
            }
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

            //_CreateNode(oArg.Node);
            //this.Canvas.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Render, new Action<Node>(_ThreadRefreshConnection), oArg.Node);
        }

        private void _OnCommentCreated(EventArg arg)
        {
            CommentCreatedArg oArg = arg as CommentCreatedArg;
            if (oArg.Comment == null)
                return;

            ///> move the comment to the topleft of the canvas
            oArg.Comment.Geo.Pos = new Point(
                -m_CurPageData.TranslateTransform.X / m_CurPageData.ScaleTransform.ScaleX,
                -m_CurPageData.TranslateTransform.Y / m_CurPageData.ScaleTransform.ScaleY);
            oArg.Comment.OnGeometryChanged();
        }

        public void ClearCanvas()
        {
            //RenderMgr.Instance.ClearNodes();
        }
        private void TabController_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source is TabControl)
            {
                if (e.AddedItems.Count > 0)
                {
                    foreach (TabItem tab in e.AddedItems)
                    {
                        //LogMgr.Instance.Log("Tab selected: " + tab.Header);
                        if (WorkBenchMgr.Instance.Switch(tab.Content as WorkBench))
                        {
                            m_CurPageData = m_PageDataDic[tab];
                            this.canvas.RenderTransform = m_CurPageData.TransGroup;
                            return;
                        }
                    }

                    LogMgr.Instance.Error("Tab switch failed.");
                }

                m_CurPageData = null;
            }
        }

        private void _MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (m_CurPageData == null)
                return;

            Point pos = e.GetPosition(this.CanvasBoard);
            Point oldPos = new Point(m_CurPageData.TranslateTransform.X, m_CurPageData.TranslateTransform.Y);

            double width = this.canvas.ActualWidth;
            double height = this.canvas.ActualHeight;

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
            if (m_CurPageData == null)
                return;

            m_CurPageData.TranslateTransform.X += delta.X;
            m_CurPageData.TranslateTransform.Y += delta.Y;
        }
        void _OnClick()
        {
            Focus();
            SelectionMgr.Instance.Clear();
        }
        public void ResetTransform()
        {
            if (m_CurPageData == null)
                return;
            m_CurPageData.ScaleTransform.ScaleX = 0;
            m_CurPageData.ScaleTransform.ScaleY = 0;
            m_CurPageData.TranslateTransform.X = 0;
            m_CurPageData.TranslateTransform.Y = 0;
        }

        private void _OnMakeCenter(EventArg arg)
        {
            if (m_CurPageData == null)
                return;

            m_MakingCenterDes.X = 0;
            m_MakingCenterDes.Y = 0;

            CompositionTarget.Rendering -= MakingCenter;
            CompositionTarget.Rendering += MakingCenter;
        }

        Point m_MakingCenterDes;
        private void MakingCenter(object sender, EventArgs e)
        {
            if (m_CurPageData == null)
                return;

            Point newDes = new Point();
            if (_MakeCenter(ref newDes))
            {
                m_MakingCenterDes = newDes;
            }

            Point curPos = new Point(m_CurPageData.TranslateTransform.X, m_CurPageData.TranslateTransform.Y);
            if ((curPos - m_MakingCenterDes).LengthSquared < 1)
            {
                CompositionTarget.Rendering -= MakingCenter;
                return;
            }

            Vector delta = m_MakingCenterDes - curPos;
            double sqrLength = delta.LengthSquared;
            double speed = 30.0;
            if (sqrLength > speed * speed)
            {
                delta = delta / Math.Sqrt(sqrLength) * speed;
            }
            m_CurPageData.TranslateTransform.X += delta.X;
            m_CurPageData.TranslateTransform.Y += delta.Y;
        }

        bool _MakeCenter(ref Point newDes)
        {
            Vector halfcanvas = new Vector(this.CanvasBoard.ActualWidth / 2, this.CanvasBoard.ActualHeight / 2);
            Point curPos = new Point(0, 0) + halfcanvas;
            Point nodesPos = new Point(m_CurPageData.TranslateTransform.X, m_CurPageData.TranslateTransform.Y);
            double nodesScale = m_CurPageData.ScaleTransform.ScaleX;
            double sqrradius = Math.Max(halfcanvas.X, halfcanvas.Y);
            sqrradius *= sqrradius;

            Point nextPos = new Point(0, 0);
            int count = 0;
            foreach (Renderer renderer in WorkBenchMgr.Instance.ActiveWorkBench.NodeList)
            {
                Point pos = new Vector(renderer.Geo.Pos.X * nodesScale, renderer.Geo.Pos.Y * nodesScale) + nodesPos;
                if ((pos - curPos).LengthSquared < sqrradius)
                {
                    ///> Much Larger Weight
                    count += 50;
                    nextPos.X += (pos.X * 50);
                    nextPos.Y += (pos.Y * 50);
                }
                else
                {
                    ///> Normal Weight
                    ++count;
                    nextPos.X += (pos.X);
                    nextPos.Y += (pos.Y);
                }
            }

            if (count > 0)
            {
                nextPos.X /= count;
                nextPos.Y /= count;

                Vector delta = nextPos - curPos;

                newDes.X = m_CurPageData.TranslateTransform.X - delta.X;
                newDes.Y = m_CurPageData.TranslateTransform.Y - delta.Y;
                return true;
            }
            return false;
        }
    }
}
