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
using YBehavior.Editor.Core.New;

namespace YBehavior.Editor
{
    /// <summary>
    /// NodeListFrame.xaml 的交互逻辑
    /// </summary>
    public partial class NodeListFrame : UserControl
    {
        public class NodeInfo : IComparable<NodeInfo>, IEquatable<NodeInfo>
        {
            private DelayableNotificationCollection<NodeInfo> m_children = new DelayableNotificationCollection<NodeInfo>();
            public DelayableNotificationCollection<NodeInfo> Children { get { return m_children; } }
            public string Name { get; set; }
            public string Icon { get; set; }
            public TreeNode Source { get { return m_Source; } }
            TreeNode m_Source;
            NodeHierachy m_Hierachy;
            int m_Level;
            public string Description { get; set; }

            public bool bIsFolder { get { return m_Hierachy != NodeHierachy.NH_None; } }
            private bool exp = false;
            public bool Expanded
            {
                get { return exp; }
                set { exp = value; }
            }

            public int CompareTo(NodeInfo other)
            {
                if (other.bIsFolder == bIsFolder)
                    return 0;
                if (bIsFolder)
                    return -1;
                return 1;
            }

            /// <summary>
            /// Just for sort
            /// </summary>
            /// <param name="other"></param>
            /// <returns></returns>
            public bool Equals(NodeInfo other)
            {
                return this == other;
            }

            ///> Folder
            public NodeInfo(NodeHierachy hierachy, int level)
            {
                m_Hierachy = hierachy;
                m_Source = null;
                Name = Core.DescriptionMgr.Instance.GetHierachyDescription((int)hierachy);
                m_Level = level;
                Icon = "📁";
                Description = null;
            }

            ///> Node
            public NodeInfo(TreeNode data)
            {
                m_Source = data;
                m_Hierachy = NodeHierachy.NH_None;
                Name = data.Name;
                Icon = "▶";
                Description = data.Description;
            }

            public void Build(TreeNode data, HashSet<string> expandedItems)
            {
                if (data == null || data.Type == TreeNodeType.TNT_Root)
                    return;

                NodeInfo child;
                if (m_Hierachy == data.Hierachy)
                {
                    child = new NodeInfo(data);
                    m_children.Add(child);
                }
                else
                {
                    int nextLevel = m_Level + 1;
                    NodeHierachy subHierachy = (NodeHierachy)((int)data.Hierachy % (int)Math.Pow(10, nextLevel));
                    child = null;
                    foreach (var chi in m_children)
                    {
                        if (chi.m_Hierachy == subHierachy)
                        {
                            child = chi;
                            break;
                        }
                    }
                    if (child == null)
                    {
                        child = new NodeInfo(subHierachy, nextLevel);
                        child.Expanded = expandedItems.Contains(child.Name);
                        m_children.Add(child);
                    }
                    child.Build(data, expandedItems);
                }
            }

            public void Sort()
            {
                m_children.Sort();
                foreach (var i in m_children)
                {
                    i.Sort();
                }
            }
        }

        HashSet<string> m_ExpandedItems = new HashSet<string>();
        NodeInfo m_NodeInfos;
        public NodeListFrame()
        {
            InitializeComponent();
            m_NodeInfos = new NodeInfo(NodeHierachy.NH_None, 0);
            this.Nodes.ItemsSource = m_NodeInfos.Children;
            _FilterNodes(null);
        }

        private void _FilterNodes(string keyword)
        {
            _GetExpandedItems(this.Nodes, m_ExpandedItems);

            using (var handler = m_NodeInfos.Children.Delay())
            {
                m_NodeInfos.Children.Clear();
                if (!string.IsNullOrEmpty(keyword))
                    keyword = keyword.ToLower();
                foreach (var node in TreeNodeMgr.Instance.NodeList)
                {
                    if (!string.IsNullOrEmpty(keyword) && !node.Name.ToLower().Contains(keyword))
                        continue;
                    m_NodeInfos.Build(node, m_ExpandedItems);
                }
            }

            m_NodeInfos.Sort();
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
                if (childControl is TreeViewItem item)
                {
                    if (item.DataContext is NodeInfo info)
                    {
                        if (item.IsExpanded)
                            expandedItems.Add(info.Name);
                        else
                            expandedItems.Remove(info.Name);
                    }
                }
            }
        }

        private void OnNodesItemDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DebugMgr.Instance.IsDebugging())
                return;
            DependencyObject obj = (DependencyObject)e.OriginalSource;
            while (obj != null && obj != this.Nodes)
            {
                if (obj.GetType() == typeof(TreeViewItem))
                {
                    NodeInfo item = this.Nodes.SelectedItem as NodeInfo;
                    if (item != null && !item.bIsFolder)
                    {
                        string nodeText = item.Name;

                        Core.New.WorkBenchMgr.Instance.CreateNodeToBench(item.Source);
                    }
                    break;
                }
                obj = VisualTreeHelper.GetParent(obj);
            }


        }

        private void AddComment_Click(object sender, RoutedEventArgs e)
        {
            if (DebugMgr.Instance.IsDebugging())
                return;
            Core.New.WorkBenchMgr.Instance.CreateComment();
        }

        private void ClearSearch_Click(object sender, RoutedEventArgs e)
        {
            this.SearchText.Text = string.Empty;
        }

        private void SearchText_TextChanged(object sender, TextChangedEventArgs e)
        {
            _FilterNodes(this.SearchText.Text);
        }
    }

}
