using System;
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
            public List<FileInfo> children { get { return m_children; } }
            public string name { get; set; }
            public string icon { get; set; }
            TreeMgr.TreeFileInfo source;

            public void Build(TreeMgr.TreeFileInfo data)
            {
                children.Clear();

                if (data == null)
                    return;

                source = data;
                name = data.name;
                icon = !data.bIsFolder ? "Resources/ICON__0000_46.png"
                                        : "Resources/ICON__0009_37.png";

                if (data.children == null)
                    return;

                foreach(TreeMgr.TreeFileInfo child in data.children)
                {
                    FileInfo info = new FileInfo();
                    this.children.Add(info);
                    info.Build(child);
                }
            }
        }

        public WorkingSpaceFrame()
        {
            InitializeComponent();
            m_FileInfos.Build(TreeMgr.Instance.GetAllTrees());
            this.Files.ItemsSource = m_FileInfos.children;
        }

        private void onFilesItemDoubleClick(object sender, MouseButtonEventArgs e)
        {
            DependencyObject obj = (DependencyObject)e.OriginalSource;
            while (obj != null && obj != this.Files)
            {
                if (obj.GetType() == typeof(TreeViewItem))
                {
                    FileInfo item = this.Files.SelectedItem as FileInfo;
                    string nodeText = item.name;

                    MessageBox.Show(nodeText);

                    break;
                }
                obj = VisualTreeHelper.GetParent(obj);
            }


        }
    }
}
