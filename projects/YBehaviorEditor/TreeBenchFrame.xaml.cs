using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using YBehavior.Editor.Core.New;

namespace YBehavior.Editor
{
    public class TreeNodeTypeTemplateSelector : DataTemplateSelector
    {
        public DataTemplate NormalTemplate { get; set; }
        public DataTemplate RootTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            TreeNode node = (item as TreeNodeRenderer).TreeOwner;
            if (node is RootTreeNode)
                return RootTemplate;
            return NormalTemplate;
        }
    }
    public class ConnectionTypeTemplateSelector : DataTemplateSelector
    {
        public DataTemplate NormalTemplate { get; set; }
        public DataTemplate WeakTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            Connection conn = (item as ConnectionRenderer).Owner;
            if (conn.Ctr.From.GetPosType == Connector.PosType.OUTPUT)
                return WeakTemplate;
            return NormalTemplate;
        }
    }

    /// <summary>
    /// WorkBenchFrame.xaml 的交互逻辑
    /// </summary>
    public partial class TreeBenchFrame : WorkBenchFrame
    {
        public override FrameworkElement GetCanvasBoard { get { return CanvasBoard; } }
        public override FrameworkElement GetCanvas { get { return canvas; } }


        public TreeBenchFrame()
        {
            InitializeComponent();
            _Init();
        }

        public override void OnWorkBenchLoaded(WorkBench bench)
        {
            base.OnWorkBenchLoaded(bench);

            if (bench != null)
            {
                //if (DebugMgr.Instance.IsDebugging(oArg.Bench.FileInfo.Name) && DebugMgr.Instance.bBreaked)
                //{
                //    _RefreshMainTreeDebug(false, NetworkMgr.Instance.MessageProcessor.TickResultToken);
                //}

                bench.NodeList.ReAdd();
                bench.ConnectionList.ReAdd();
                this.nodeLayer.ItemsSource = bench.NodeList.Collection;
                this.commentLayer.ItemsSource = bench.Comments;
                this.connectionLayer.ItemsSource = bench.ConnectionList.Collection;

                m_MakingCenterDes = (bench as TreeBench).Tree.Root.Geo.Pos;
                m_MakingCenterDes = new Point(-m_MakingCenterDes.X, -m_MakingCenterDes.Y);
            }
            else
            {
                this.nodeLayer.ItemsSource = null;
                this.commentLayer.ItemsSource = null;
                this.connectionLayer.ItemsSource = null;
                m_MakingCenterDes = new Point();
            }
        }

        public override void OnWorkBenchSelected()
        {
            base.OnWorkBenchSelected();
            DraggingConnection<UIConnection>.Instance.SetCanvas(this.canvas);
        }
    }
}
