using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using YBehavior.Editor.Core.New;

namespace YBehavior.Editor
{
    /// <summary>
    /// WorkingSpaceFrame.xaml 的交互逻辑
    /// </summary>
    public partial class WorkingSpaceFrame : UserControl
    {
        FileInfo m_FileInfos = new FileInfo();

        public class FileInfo
        {
            private DelayableNotificationCollection<FileInfo> m_children = new DelayableNotificationCollection<FileInfo>();
            public DelayableNotificationCollection<FileInfo> Children { get { return m_children; } }
            public string Name { get; set; }
            public string Icon { get; set; }
            TreeFileMgr.TreeFileInfo source;
            public TreeFileMgr.TreeFileInfo Source { get { return source; } }
            private bool exp = false;
            public bool Expanded
            {
                get { return exp; }
                set
                {
                    exp = value;
                }
            }

            public void Build(TreeFileMgr.TreeFileInfo data, HashSet<string> expandedItems = null)
            {
                using (var handler = Children.Delay())
                {
                    Children.Clear();

                    if (data == null)
                        return;

                    source = data;
                    Name = data.Name;
                    Icon = data.FileType != FileType.FOLDER ? "📃"
                                            : "📁";

                    if (Name == null)
                        Expanded = true;
                    else
                        Expanded = expandedItems != null ? expandedItems.Contains(Name) : false;

                    if (data.Children == null)
                        return;

                    foreach (TreeFileMgr.TreeFileInfo child in data.Children)
                    {
                        FileInfo info = new FileInfo();
                        this.Children.Add(info);
                        info.Build(child, expandedItems);
                    }
                }
            }
        }

        public WorkingSpaceFrame()
        {
            InitializeComponent();
            EventMgr.Instance.Register(EventType.WorkBenchSaved, _OnWorkBenchSaved);
            EventMgr.Instance.Register(EventType.NetworkConnectionChanged, _OnDebugTargetChanged);
            EventMgr.Instance.Register(EventType.DebugTargetChanged, _OnDebugTargetChanged);

            _InitWorkingSpace();

            this.Files.ItemsSource = m_FileInfos.Children;

        }

        void _GetExpandedItems(ItemsControl items, HashSet<string> expandedItems)
        {
            foreach (object obj in items.Items)
            {
                ItemsControl childControl = items.ItemContainerGenerator.ContainerFromItem(obj) as ItemsControl;
                if (childControl != null)
                {
                    _GetExpandedItems(childControl, expandedItems);
                }
                if (childControl is TreeViewItem item && item.IsExpanded)
                {
                    if (item.DataContext is FileInfo info)
                        expandedItems.Add(info.Name);
                }
            }
        }

        HashSet<string> m_ExpandedItems = new HashSet<string>();
        private void _RefreshWorkingSpace(bool bReload)
        {
            m_ExpandedItems.Clear();
            _GetExpandedItems(this.Files, m_ExpandedItems);

            m_FileInfos.Build(bReload ? TreeFileMgr.Instance.ReloadAndGetAllTrees() : TreeFileMgr.Instance.AllTrees, m_ExpandedItems);
//            this.Files.ItemsSource = m_FileInfos.Children;
        }

        private void _InitWorkingSpace()
        {
            m_ExpandedItems.Clear();
            string expandedFolders = Config.Instance.ExpandedFolders;
            string[] folders = expandedFolders.Split(new char[] { '|' });
            foreach (string s in folders)
            {
                m_ExpandedItems.Add(s);
            }

            m_FileInfos.Build(TreeFileMgr.Instance.ReloadAndGetAllTrees(), m_ExpandedItems);
        }

        private void OnFilesItemDoubleClick(object sender, MouseButtonEventArgs e)
        {
            DependencyObject obj = (DependencyObject)e.OriginalSource;
            while (obj != null && obj != this.Files)
            {
                if (obj.GetType() == typeof(TreeViewItem))
                {
                    FileInfo item = this.Files.SelectedItem as FileInfo;
                    if (item != null && item.Source.Path != null)
                    {
                        string nodeText = item.Name;

                        WorkBench bench = null;
                        if ((bench = WorkBenchMgr.Instance.OpenWorkBench(item.Source)) != null)
                        {
                            WorkBenchLoadedArg arg = new WorkBenchLoadedArg();
                            arg.Bench = bench;
                            EventMgr.Instance.Send(arg);
                        }
                    }
                    break;
                }
                obj = VisualTreeHelper.GetParent(obj);
            }


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
                _RefreshWorkingSpace(false);
            }
        }

        private void _OnDebugTargetChanged(EventArg arg)
        {
            this.Dispatcher.BeginInvoke(new Action
                (() =>
                {
                    this.Files.IsEnabled = !DebugMgr.Instance.IsDebugging();
                    this.FileOperatePanel.IsEnabled = !DebugMgr.Instance.IsDebugging();
                })
            );
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            /*int res = */
            WorkBenchMgr.Instance.TrySaveAndExport();

            //if (res == 1)
            //    _RefreshWorkingSpace(false);
        }

        private void btnNewTree_Click(object sender, RoutedEventArgs e)
        {
            WorkBench bench = null;
            if ((bench = WorkBenchMgr.Instance.CreateNewBench(FileType.TREE)) != null)
            {
                WorkBenchLoadedArg arg = new WorkBenchLoadedArg();
                arg.Bench = bench;
                EventMgr.Instance.Send(arg);
            }
        }

        private void btnNewFSM_Click(object sender, RoutedEventArgs e)
        {
            WorkBench bench = null;
            if ((bench = WorkBenchMgr.Instance.CreateNewBench(FileType.FSM)) != null)
            {
                WorkBenchLoadedArg arg = new WorkBenchLoadedArg();
                arg.Bench = bench;
                EventMgr.Instance.Send(arg);
            }
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            _RefreshWorkingSpace(true);
        }

        private void Files_LostFocus(object sender, RoutedEventArgs e)
        {
            m_ExpandedItems.Clear();
            _GetExpandedItems(this.Files, m_ExpandedItems);
            StringBuilder sb = new StringBuilder();
            foreach (string s in m_ExpandedItems)
            {
                if (sb.Length > 0)
                    sb.Append('|');
                sb.Append(s);
            }
            Config.Instance.ExpandedFolders = sb.ToString();
        }
    }
}
