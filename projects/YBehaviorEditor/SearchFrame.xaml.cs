using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using YBehavior.Editor.Core.New;

namespace YBehavior.Editor
{
    /// <summary>
    /// SearchWindow.xaml 的交互逻辑
    /// </summary>
    public partial class SearchFrame : UserControl
    {
        public string SearchIndexCount
        {
            get
            {
                return string.Format("{0}/{1}", m_Index + 1, m_Results.Count);
            }
        }

        string m_SearchingText;
        List<NodeBaseRenderer> m_Results = new List<NodeBaseRenderer>();
        int m_Index = 0;
        public SearchFrame()
        {
            InitializeComponent();
        }

        void _UpdateSearchIndexCount()
        {
            this.Info.Text = SearchIndexCount;
        }
        private void Previous_Click(object sender, RoutedEventArgs e)
        {
            _SearchPrevious();
        }

        private void _SearchPrevious()
        {
            if (!_TrySearch())
            {
                --m_Index;
                if (m_Index < 0)
                    m_Index = m_Results.Count - 1;

                _Select();
            }
            _UpdateSearchIndexCount();
        }

        private void Next_Click(object sender, RoutedEventArgs e)
        {
            _SearchNext();
        }

        private void _SearchNext()
        {
            if (!_TrySearch())
            {
                ++m_Index;
                if (m_Index >= m_Results.Count)
                    m_Index = 0;

                _Select();
            }
            _UpdateSearchIndexCount();
        }

        private void Search_Click(object sender, RoutedEventArgs e)
        {
            _Search();
            _UpdateSearchIndexCount();
        }

        bool _TrySearch()
        {
            if (this.Input.Text != m_SearchingText)
            {
                _Search();
                return true;
            }
            return false;
        }

        void _Search()
        {
            m_SearchingText = this.Input.Text.ToLower();
            m_Results.Clear();
            m_Index = 0;

            WorkBench bench = WorkBenchMgr.Instance.ActiveWorkBench;
            if (bench == null)
                return;

            if (bench is TreeBench)
            {
                _SearchTree(bench);
            }
            else
            {
                _SearchFSM(bench);
            }

            _Select();
        }

        void _SearchTree(WorkBench bench)
        {
            foreach (var r in bench.NodeList.Collection)
            {
                TreeNodeRenderer renderer = r as TreeNodeRenderer;
                if (renderer.FullName.ToLower().Contains(m_SearchingText))
                {
                    m_Results.Add(r);
                    continue;
                }

                foreach (var v in renderer.TreeOwner.Variables.Datas)
                {
                    if (v.Variable.Value != null && v.Variable.Value.ToLower().Contains(m_SearchingText))
                    {
                        m_Results.Add(r);
                        break;
                    }
                }
            }
        }
        void _SearchFSM(WorkBench bench)
        {
            foreach (var r in bench.NodeList.Collection)
            {
                FSMStateRenderer renderer = r as FSMStateRenderer;
                if (renderer.NickName == m_SearchingText)
                {
                    m_Results.Add(r);
                    continue;
                }
            }
        }

        void _Select()
        {
            if (m_Index < 0 || m_Index >= m_Results.Count)
                return;

            WorkBench bench = WorkBenchMgr.Instance.ActiveWorkBench;
            if (bench == null)
                return;

            var renderer = m_Results[m_Index];

            foreach (var r in bench.NodeList.Collection)
            {
                if (r == renderer)
                {
                    r.SelectTrigger = !r.SelectTrigger;
                    EventMgr.Instance.Send(new MakeCenterArg()
                    {
                        Target = r
                    });
                    return;
                }
            }

            EventMgr.Instance.Send(new ShowSystemTipsArg()
            {
                TipType = ShowSystemTipsArg.TipsType.TT_Error,
                Content = "Cant find the searched node here. Maybe the node has been deleted, or here is another file."
            });
        }

        private void UserControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (this.Visibility == Visibility.Visible)
            {
                this.Dispatcher.BeginInvoke((System.Action)delegate
                {
                    System.Windows.Input.Keyboard.Focus(this.Input);
                }, System.Windows.Threading.DispatcherPriority.Render);
            }
        }

        private void Input_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
                _SearchNext();
        }
    }
}
