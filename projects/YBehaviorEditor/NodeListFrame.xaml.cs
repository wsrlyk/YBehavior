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
    /// NodeListFrame.xaml 的交互逻辑
    /// </summary>
    public partial class NodeListFrame : UserControl
    {
        public class NodeInfo
        {
            private List<NodeInfo> m_children = new List<NodeInfo>();
            public List<NodeInfo> Children { get { return m_children; } }
            public string Name { get; set; }
            public string Icon { get; set; }
            public Node Source { get { return m_Source; } }
            Node m_Source;
            NodeHierachy m_Hierachy;
            int m_Level;

            public bool bIsFolder { get { return m_Hierachy != NodeHierachy.NH_None; } }
            ///> Folder
            public NodeInfo(NodeHierachy hierachy, int level)
            {
                m_Hierachy = hierachy;
                m_Source = null;
                Name = m_Hierachy.ToString();
                m_Level = level;
                Icon = "Resources/ICON__0009_37.png";
            }

            ///> Node
            public NodeInfo(Node data)
            {
                m_Source = data;
                m_Hierachy = NodeHierachy.NH_None;
                Name = data.Name;
                Icon = "Resources/ICON__0000_46.png";
            }

            public void Build(Node data)
            {
                if (data == null)
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
                        m_children.Add(child);
                    }
                    child.Build(data);
                }
            }
        }

        NodeInfo m_NodeInfos;
        public NodeListFrame()
        {
            InitializeComponent();
            m_NodeInfos = new NodeInfo(NodeHierachy.NH_None, 0);
            foreach(var node in Core.NodeMgr.Instance.NodeList)
            {
                m_NodeInfos.Build(node);
            }
            this.Nodes.ItemsSource = m_NodeInfos.Children;
        }

        private void OnNodesItemDoubleClick(object sender, MouseButtonEventArgs e)
        {
            DependencyObject obj = (DependencyObject)e.OriginalSource;
            while (obj != null && obj != this.Nodes)
            {
                if (obj.GetType() == typeof(TreeViewItem))
                {
                    NodeInfo item = this.Nodes.SelectedItem as NodeInfo;
                    if (item != null && !item.bIsFolder)
                    {
                        string nodeText = item.Name;

                        Node node = null;
                        if ((node = WorkBenchMgr.Instance.AddNodeToBench(item.Source)) != null)
                        {
                            NewNodeAddedArg arg = new NewNodeAddedArg();
                            arg.Node = node;
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
