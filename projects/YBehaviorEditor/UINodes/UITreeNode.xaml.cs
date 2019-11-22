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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using YBehavior.Editor.Core.New;

namespace YBehavior.Editor
{
    /// <summary>
    /// BehaviorNode.xaml 的交互逻辑
    /// </summary>
    public abstract class UITreeNodeBase : UINodeBase<TreeNode, TreeNodeRenderer>
    {
    }

    public partial class UITreeNode : UITreeNodeBase
    {
        public override FrameworkElement SelectCoverUI { get { return this.selectCover; } }
        public override Brush OutlookBrush
        {
            get { return this.border.BorderBrush; }
            set
            {
                this.border.BorderBrush = value;
            }
        }
        public override FrameworkElement CommentUI { get { return commentBorder; } }
        public override FrameworkElement DebugUI { get { return debugCover; } }
        public override Brush DebugBrush
        {
            get { return this.debugCover.BorderBrush; }
            set
            {
                this.debugCover.BorderBrush = value;
            }
        }

        public UITreeNode()
        {
            InitializeComponent();
            _Init();
        }

        protected override void _OnDataContextChanged()
        {
            _CreateConnectors();
            _SetCommentPos();
        }

        private void _CreateConnectors()
        {
            m_uiConnectors.Clear();
            topConnectors.Children.Clear();
            bottomConnectors.Children.Clear();
            leftConnectors.Child = null;

            foreach (Connector ctr in Node.Conns.ConnectorsList)
            {
                //if (ctr is ConnectorNone)
                //    continue;

                TreeUIConnector uiConnector = new TreeUIConnector
                {
                    Title = ctr.Identifier,
                    Ctr = ctr
                };
                //uiConnector.SetCanvas(m_Canvas);
                if (ctr.Identifier == Connector.IdentifierCondition)
                {
                    leftConnectors.Child = uiConnector;
                }
                else
                    bottomConnectors.Children.Add(uiConnector);

                m_uiConnectors.Add(ctr.Identifier, uiConnector);
            }

            if (Node.Conns.ParentConnector != null)
            {
                TreeUIConnector uiConnector = new TreeUIConnector
                {
                    Title = Node.Icon,
                    Ctr = Node.Conns.ParentConnector
                };
                //uiConnector.SetCanvas(m_Canvas);
                uiConnector.title.FontSize = 14;
                topConnectors.Children.Add(uiConnector);

                m_uiConnectors.Add(Connector.IdentifierParent, uiConnector);
            }
        }

        private void _SetCommentPos()
        {
            if (bottomConnectors.Children.Count > 0)
            {
                DockPanel.SetDock(commentBorder, Dock.Right);
                commentBorder.Margin = new Thickness(0, this.topConnectors.Height, 0, bottomConnectors.Height);
            }
            else
            {
                DockPanel.SetDock(commentBorder, Dock.Bottom);
                commentBorder.Margin = new Thickness(0);
            }
        }

        protected override void _OnSelect(bool bSelect)
        {
            this.Node.NodeMemory.RefreshVariables();
            if (this.Node is SubTreeNode)
            {
                (this.Node as SubTreeNode).InOutMemory.RefreshVariables();
            }
        }

        public void ToggleCondition()
        {
            Renderer.EnableCondition = !Renderer.EnableCondition;
        }

        public void ToggleFold()
        {
            if (Node.Conns.NodeCount > 0)
                Renderer.Folded = !Renderer.Folded;
        }
    }

    public class UIRootTreeNode : UITreeNode
    {

    }

    public class UINormalTreeNode : UITreeNode, ISelectable, IDeletable, IDuplicatable, IDebugPointable, ICanDisable, IHasCondition, ICanFold
    {

    }
}
