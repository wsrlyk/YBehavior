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
    /// TabBarFrame.xaml 的交互逻辑
    /// </summary>
    public partial class TabBarFrame : UserControl
    {
        Dictionary<TabItem, PageData> m_PageDataDic = new Dictionary<TabItem, PageData>();

        PageData m_CurPageData;
        WorkBenchFrame m_CurBench;

        public TabBarFrame()
        {
            InitializeComponent();
            EventMgr.Instance.Register(EventType.WorkBenchLoaded, _OnWorkBenchLoaded);
            EventMgr.Instance.Register(EventType.WorkBenchSaved, _OnWorkBenchSaved);
            EventMgr.Instance.Register(EventType.SelectWorkBench, _OnSelectWorkBench);
            EventMgr.Instance.Register(EventType.WorkBenchSelected, _OnWorkBenchSelected);

            this.TreeBench.Visibility = Visibility.Collapsed;
            this.FSMBench.Visibility = Visibility.Collapsed;
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

        void _OnWorkBenchSelected(EventArg arg)
        {
            WorkBenchSelectedArg oArg = arg as WorkBenchSelectedArg;

            WorkBenchFrame nextBench = null;
            if (oArg.Bench != null)
            {
                if (oArg.Bench is TreeBench)
                {
                    nextBench = this.TreeBench;
                }
                else if (oArg.Bench is TreeBench)
                {
                    nextBench = this.TreeBench;
                }
            }

            if (nextBench != m_CurBench)
            {
                if (m_CurBench != null)
                {
                    m_CurBench.Disable();
                    m_CurBench.Visibility = Visibility.Collapsed;
                }
                m_CurBench = nextBench;
                {
                    m_CurBench.Enable();
                    m_CurBench.Visibility = Visibility.Visible;
                }
            }

            if (m_CurBench != null)
            {
                m_CurBench.CurPageData = m_CurPageData;
                m_CurBench.OnWorkBenchSelected(arg);
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
                            m_CurBench.CurPageData = m_CurPageData;
                            m_CurBench.GetCanvas.RenderTransform = m_CurPageData.TransGroup;
                            return;
                        }
                    }

                    LogMgr.Instance.Error("Tab switch failed.");
                }

                m_CurPageData = null;
            }

        }
    }
}
