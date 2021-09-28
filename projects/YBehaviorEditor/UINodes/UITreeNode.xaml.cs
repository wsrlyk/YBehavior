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
        public static readonly DependencyProperty CenterOffsetXProperty = DependencyProperty.RegisterAttached(
            "CenterOffsetX",
            typeof(double),
            typeof(UITreeNode));
        public static readonly DependencyProperty CenterOffsetYProperty = DependencyProperty.RegisterAttached(
            "CenterOffsetY",
            typeof(double),
            typeof(UITreeNode));

        public double CenterOffsetX
        {
            get
            {
                return (double)GetValue(CenterOffsetXProperty);
            }
            set
            {
                SetValue(CenterOffsetXProperty, value);
            }
        }

        public double CenterOffsetY
        {
            get
            {
                return (double)GetValue(CenterOffsetYProperty);
            }
            set
            {
                SetValue(CenterOffsetYProperty, value);
            }
        }

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
            get { return this.debugCover.Background; }
            set
            {
                this.debugCover.Background = value;
            }
        }

        public UITreeNode()
        {
            InitializeComponent();
            _Init();

            this.SizeChanged += OnSizeChanged;
            _UpdateCenterPos();
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            _UpdateCenterPos();
        }

        void _UpdateCenterPos()
        {
            if (!this.IsAncestorOf(this.border))
                return;
            var p = this.border.TransformToAncestor(this).
                Transform(new Point(this.border.ActualWidth * 0.5f, this.border.ActualHeight * 0.5f));

            CenterOffsetX = p.X;
            CenterOffsetY = p.Y;
        }

        protected override void _OnDataContextChanged()
        {
            _CreateConnectors();
            _SetCommentPos();

            if (Config.Instance.NodeTooltipDelayTime > 0)
            {
                this.border.SetBinding(FrameworkElement.ToolTipProperty, new Binding()
                {
                    Path = new PropertyPath("Owner.Description"),
                });
                ToolTipService.SetInitialShowDelay(this.border, Config.Instance.NodeTooltipDelayTime);
            }
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

                UIConnector uiConnector;
                if (ctr.GetPosType == Connector.PosType.OUTPUT)
                {
                    uiConnector = new VariableUIConnector
                    {
                        Title = ctr.Identifier,
                        Ctr = ctr
                    };
                    outputConnectors.Children.Add(uiConnector);
                }
                else
                {
                    uiConnector = new TreeUIConnector
                    {
                        Title = ctr.Identifier,
                        Ctr = ctr
                    };
                    if (ctr.GetPosType == Connector.PosType.CONDITION)
                    {
                        leftConnectors.Child = uiConnector;
                    }
                    else
                    {
                        bottomConnectors.Children.Add(uiConnector);
                    }
                }
                m_uiConnectors.Add(ctr, uiConnector);
            }

            foreach (Connector ctr in Node.Conns.InputConnectors)
            {
                UIConnector uiConnector = new VariableUIConnector
                {
                    Title = ctr.Identifier,
                    Ctr = ctr
                };
                //uiConnector.SetCanvas(m_Canvas);

                inputConnectors.Children.Add(uiConnector);

                m_uiConnectors.Add(ctr, uiConnector);
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

                m_uiConnectors.Add(Node.Conns.ParentConnector, uiConnector);
            }
        }

        private void _SetCommentPos()
        {
            if (bottomConnectors.Children.Count > 0)
            {
                DockPanel.SetDock(commentBorder, Dock.Right);
                commentBorder.Margin = new Thickness(5, this.topConnectors.Height, 0, bottomConnectors.Height);
            }
            else
            {
                DockPanel.SetDock(commentBorder, Dock.Bottom);
                commentBorder.Margin = new Thickness(0, -10, 0, 0);
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
