using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using YBehavior.Editor.Core.New;

namespace YBehavior.Editor
{
    /// <summary>
    /// Class of keeping the translate and scale transform of a page for a workbench
    /// </summary>
    public class PageData
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
    /// Tab data containing a workbench
    /// </summary>
    public class TabData
    {
        public WorkBenchFrame Frame;
    }
    /// <summary>
    /// UI of tab bar
    /// </summary>
    public partial class TabBarFrame : UserControl
    {
        Dictionary<TabItem, TabData> m_TabDataDic = new Dictionary<TabItem, TabData>();

        TabData m_CurTabData;

        public TabBarFrame()
        {
            InitializeComponent();
            EventMgr.Instance.Register(EventType.WorkBenchLoaded, _OnWorkBenchLoaded);
            EventMgr.Instance.Register(EventType.SelectWorkBench, _OnSelectWorkBench);
            EventMgr.Instance.Register(EventType.WorkBenchClosed, _OnWorkBenchClosed);

            foreach (WorkBench openedBench in WorkBenchMgr.Instance.OpenedBenches)
            {
                _LoadWorkBench(openedBench);
            }
        }

        private void _OnWorkBenchLoaded(EventArg arg)
        {
            WorkBenchLoadedArg oArg = arg as WorkBenchLoadedArg;
            if (oArg.Bench == null)
                return;
            if (oArg.FromAutoOpen)
            {
                if (Config.Instance.NotAutoOpenFiles.Contains(oArg.Bench.FileInfo.DisplayName))
                    return;
            }
            _LoadWorkBench(oArg.Bench);
        }

        private void _LoadWorkBench(WorkBench bench)
        {
            ///> Check if the bench has already been in the tabs

            UITabItem activeTab = null;
            foreach (UITabItem tab in this.TabController.Items)
            {
                if (tab.Content == bench)
                {
                    activeTab = tab;
                    break;
                }
            }
            ///> Create new tab
            if (activeTab == null)
            {
                activeTab = new UITabItem();
                //activeTab.Header = oArg.Bench.FileInfo.DisplayName;
                activeTab.ToolTip = bench.FileInfo.DisplayPath;
                activeTab.Content = bench;
                activeTab.CloseHandler += _TabCloseClicked;
                this.TabController.Items.Add(activeTab);
                activeTab.PreviewMouseMove += TabItem_PreviewMouseMove;
                activeTab.PreviewMouseLeftButtonDown += TabItem_PreviewMouseLeftButtonDown;
                activeTab.Drop += TabItem_Drop;
                activeTab.AllowDrop = true;

                TabData tabData = new TabData();
                if (bench is TreeBench)
                    tabData.Frame = new TreeBenchFrame();
                else
                    tabData.Frame = new FSMBenchFrame();

                m_TabDataDic[activeTab] = tabData;
                this.BenchContainer.Children.Add(tabData.Frame);
                tabData.Frame.Visibility = Visibility.Collapsed;

                tabData.Frame.OnWorkBenchLoaded(bench);

                activeTab.DataContext = bench;
                activeTab.SetBinding(UITabItem.HeaderProperty, new Binding()
                {
                    Path = new PropertyPath("ShortDisplayName"),
                    Mode = BindingMode.OneWay
                });
            }

            activeTab.IsSelected = WorkBenchMgr.Instance.ActiveWorkBench == bench;

            //_RenderActiveWorkBench();
        }
        private void _OnWorkBenchClosed(EventArg arg)
        {
            WorkBenchClosedArg oArg = arg as WorkBenchClosedArg;
            WorkBench bench = oArg.Bench;
            if (bench == null)
                return;

            foreach (var pair in m_TabDataDic)
            {
                if (pair.Key.Content as WorkBench == bench)
                {
                    TabData tabData = pair.Value;
                    m_TabDataDic.Remove(pair.Key);
                    tabData.Frame.Disable();
                    BenchContainer.Children.Remove(tabData.Frame);
                    this.TabController.Items.Remove(pair.Key);

                    break;
                }
            }

        }
        private void _OnSelectWorkBench(EventArg arg)
        {
            SelectWorkBenchArg oArg = arg as SelectWorkBenchArg;

            foreach (UITabItem tab in this.TabController.Items)
            {
                if (tab.Content == oArg.Bench)
                {
                    tab.IsSelected = true;
                    break;
                }
            }
        }

        private bool _TabCloseClicked(UITabItem tab)
        {
            if (tab == null)
                return false;

            WorkBench bench = tab.Content as WorkBench;
            if (bench != null)
            {
                //if (DebugMgr.Instance.IsDebugging(bench))
                //    return false;

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
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    bench.SaveSuo();
                }
            }

            WorkBenchMgr.Instance.Remove(bench);
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
                var tabItem = e.Source as UITabItem;
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

            var tabItemSource = e.Data.GetData(typeof(UITabItem)) as UITabItem;

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
                            if (m_CurTabData != null)
                            {
                                m_CurTabData.Frame.Disable();
                                m_CurTabData.Frame.Visibility = Visibility.Collapsed;
                            }
                            m_CurTabData = m_TabDataDic[tab];

                            m_CurTabData.Frame.Visibility = Visibility.Visible;
                            m_CurTabData.Frame.Enable();
                            m_CurTabData.Frame.OnWorkBenchSelected();
                            return;
                        }
                    }

                    LogMgr.Instance.Error("Tab switch failed.");
                }

                m_CurTabData = null;
            }

        }

        private void TabController_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            ScrollViewer scrollViewer = (ScrollViewer)sender;
            scrollViewer.ScrollToHorizontalOffset(scrollViewer.HorizontalOffset - e.Delta);
            e.Handled = true;
        }
    }
}
