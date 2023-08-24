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
    /// WorkSpaceFrame.xaml 的交互逻辑
    /// </summary>
    public partial class WorkSpaceFrame : UserControl
    {
        FileInfo m_FileInfos = new FileInfo();

        public class FileInfo
        {
            private DelayableNotificationCollection<FileInfo> m_children = new DelayableNotificationCollection<FileInfo>();
            public DelayableNotificationCollection<FileInfo> Children { get { return m_children; } }
            public string Name { get; set; }
            public string Icon { get; set; }
            FileMgr.FileInfo source;
            public FileMgr.FileInfo Source { get { return source; } }
            private int m_Depth = 0;
            private bool exp = false;
            public bool Expanded
            {
                get { return exp; }
                set
                {
                    exp = value;
                }
            }

            public void Build(List<FileMgr.FileInfo> datas, string filter, HashSet<string> expandedItems = null)
            {
                using (var handler = Children.Delay())
                {
                    Children.Clear();

                    if (datas == null)
                        return;

                    foreach (var data in datas)
                    {
                        if (!string.IsNullOrEmpty(filter) && !data.Name.ToLower().Contains(filter.ToLower()))
                            continue;
                        _Build(data, expandedItems);
                    }
                }
            }
            void _Build(FileMgr.FileInfo data, HashSet<string> expandedItems = null)
            {
                if (m_Depth == data.FolderDepth)
                {
                    // Create this File
                    FileInfo info = new FileInfo();
                    switch (data.FileType)
                    {
                        case FileType.TREE:
                            info.Icon = "🌿";
                            break;
                        case FileType.FSM:
                            info.Icon = "♻";
                            break;
                        default:
                            break;
                    }
                    info.source = data;
                    info.Name = data.Name;
                    this.Children.Add(info);
                }
                else
                {
                    // Create Sub Folder If Not Exist
                    FileInfo folder = null;
                    foreach (FileInfo child in this.Children)
                    {
                        if (child.source != null)
                            continue;

                        if (child.Name == data.FolderStack[m_Depth])
                        {
                            folder = child;
                            break;
                        }
                    }
                    if (folder == null)
                    {
                        folder = new FileInfo();
                        folder.Icon = "📁";
                        folder.m_Depth = m_Depth + 1;
                        folder.Name = data.FolderStack[m_Depth];
                        this.Children.Add(folder);
                        folder.Expanded = expandedItems != null ? expandedItems.Contains(folder.Name) : false;
                    }
                    folder._Build(data, expandedItems);
                }
            }
        }

        public WorkSpaceFrame()
        {
            InitializeComponent();
            EventMgr.Instance.Register(EventType.WorkBenchSaved, _OnWorkBenchSaved);
            EventMgr.Instance.Register(EventType.NetworkConnectionChanged, _OnDebugTargetChanged);
            EventMgr.Instance.Register(EventType.DebugTargetChanged, _OnDebugTargetChanged);
            EventMgr.Instance.Register(EventType.ShowWorkSpace, _OnShow);

            _InitWorkSpace();

            this.Files.ItemsSource = m_FileInfos.Children;

            if (WorkBenchMgr.Instance.OpenedBenches.Count > 0)
                _Hide();
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
                if (childControl is TreeViewItem item && item.DataContext is FileInfo info)
                {
                    if (item.IsExpanded && info.Source == null)
                    {
                        expandedItems.Add(info.Name);
                    }
                    else
                    {
                        expandedItems.Remove(info.Name);
                    }
                }
            }
        }

        HashSet<string> m_ExpandedItems = new HashSet<string>();
        private void _RefreshWorkSpace(bool bReload)
        {
            if (bReload)
                m_ExpandedItems.Clear();
            _GetExpandedItems(this.Files, m_ExpandedItems);

            m_FileInfos.Build(bReload ? FileMgr.Instance.ReloadAndGetAllFiles() : FileMgr.Instance.AllFiles, _Filter, m_ExpandedItems);
//            this.Files.ItemsSource = m_FileInfos.Children;
        }

        private void _InitWorkSpace()
        {
            ////m_ExpandedItems.Clear();
            ////string expandedFolders = Config.Instance.ExpandedFolders;
            ////string[] folders = expandedFolders.Split(new char[] { '|' });
            ////foreach (string s in folders)
            ////{
            ////    m_ExpandedItems.Add(s);
            ////}
            m_ExpandedItems = Config.Instance.ExpandedFolders;
            m_FileInfos.Build(FileMgr.Instance.AllFiles, _Filter, m_ExpandedItems);
        }

        private void OnFilesItemDoubleClick(object sender, MouseButtonEventArgs e)
        {
            DependencyObject obj = (DependencyObject)e.OriginalSource;
            while (obj != null && obj != this.Files)
            {
                if (obj.GetType() == typeof(TreeViewItem))
                {
                    FileInfo item = this.Files.SelectedItem as FileInfo;
                    if (item != null && item.Source != null && item.Source.Path != null)
                    {
                        string nodeText = item.Name;

                        WorkBench bench = null;
                        if ((bench = WorkBenchMgr.Instance.OpenWorkBench(item.Source)) != null)
                        {
                            _Hide();
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
            ///> Rename the tab title, refresh the references to the TreeMgr
            //if (oArg.bCreate)
            {
                _RefreshWorkSpace(false);
            }
        }

        private void _OnDebugTargetChanged(EventArg arg)
        {
            this.Dispatcher.BeginInvoke(new Action
                (() =>
                {
                    //this.Files.IsEnabled = !DebugMgr.Instance.IsDebugging();
                    this.FileOperatePanel.IsEnabled = !NetworkMgr.Instance.IsConnected;
                })
            );
        }

        private void _OnShow(EventArg arg)
        {
            this.Visibility = Visibility.Visible;
            this.SearchText.SetFocus();
        }

        private void btnNewTree_Click(object sender, RoutedEventArgs e)
        {
            WorkBench bench = null;
            if ((bench = WorkBenchMgr.Instance.CreateNewBench(FileType.TREE)) != null)
            {
                WorkBenchLoadedArg arg = new WorkBenchLoadedArg();
                arg.Bench = bench;
                EventMgr.Instance.Send(arg);
                _Hide();
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
                _Hide();
            }
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            _RefreshWorkSpace(true);
        }

        private void Files_LostFocus(object sender, RoutedEventArgs e)
        {
            //m_ExpandedItems.Clear();
            _GetExpandedItems(this.Files, m_ExpandedItems);
            ////StringBuilder sb = new StringBuilder();
            ////foreach (string s in m_ExpandedItems)
            ////{
            ////    if (sb.Length > 0)
            ////        sb.Append('|');
            ////    sb.Append(s);
            ////}
            ////Config.Instance.ExpandedFolders = sb.ToString();
        }

        private string _Filter { get { return this.SearchText.Text; } }

        private void SearchText_TextChanged(object sender, TextChangedEventArgs e)
        {
            _RefreshWorkSpace(false);
        }

        private void Border_MouseUp(object sender, MouseButtonEventArgs e)
        {
            _Hide();
        }

        void _Hide()
        {
            this.Visibility = Visibility.Collapsed;
        }
    }
}
