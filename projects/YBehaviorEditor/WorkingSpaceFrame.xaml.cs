﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
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
    /// WorkingSpaceFrame.xaml 的交互逻辑
    /// </summary>
    public partial class WorkingSpaceFrame : UserControl
    {
        FileInfo m_FileInfos = new FileInfo();

        public class FileInfo
        {
            private List<FileInfo> m_children = new List<FileInfo>();
            public List<FileInfo> Children { get { return m_children; } }
            public string Name { get; set; }
            public string Icon { get; set; }
            TreeFileMgr.TreeFileInfo source;
            public TreeFileMgr.TreeFileInfo Source { get { return source; } }

            public void Build(TreeFileMgr.TreeFileInfo data)
            {
                Children.Clear();

                if (data == null)
                    return;

                source = data;
                Name = data.Name;
                Icon = !data.bIsFolder ? "Resources/ICON__0000_46.png"
                                        : "Resources/ICON__0009_37.png";

                if (data.Children == null)
                    return;

                foreach(TreeFileMgr.TreeFileInfo child in data.Children)
                {
                    FileInfo info = new FileInfo();
                    this.Children.Add(info);
                    info.Build(child);
                }
            }
        }

        public WorkingSpaceFrame()
        {
            InitializeComponent();
            m_FileInfos.Build(TreeFileMgr.Instance.GetAllTrees());
            this.Files.ItemsSource = m_FileInfos.Children;
        }

        private void OnFilesItemDoubleClick(object sender, MouseButtonEventArgs e)
        {
            DependencyObject obj = (DependencyObject)e.OriginalSource;
            while (obj != null && obj != this.Files)
            {
                if (obj.GetType() == typeof(TreeViewItem))
                {
                    FileInfo item = this.Files.SelectedItem as FileInfo;
                    if (item != null)
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
    }
}
