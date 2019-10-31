using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using YBehavior.Editor.Core.New;

namespace YBehavior.Editor
{
    /// <summary>
    /// BehaviorNode.xaml 的交互逻辑
    /// </summary>
    public partial class UIFSMState : YUserControl, ISelectable, IDeletable, IDuplicatable, IDebugPointable, ICanDisable
    {
        static SelectionStateChangeHandler defaultSelectHandler = SelectionMgr.Instance.OnSingleSelectedChange;

        public SelectionStateChangeHandler SelectHandler { get; set; }

        public FSMStateNode Node { get; set; }
        public FSMStateRenderer Renderer { get; set; }

        Operation m_Operation;

        Dictionary<string, UIConnector> m_uiConnectors = new Dictionary<string, UIConnector>();

        public UIFSMState()
        {
            InitializeComponent();
            this.selectCover.Visibility = Visibility.Collapsed;

            SelectHandler = defaultSelectHandler;

            m_Operation = new Operation(this);
            m_Operation.RegisterLeftClick(_OnClick);
            m_Operation.RegisterLeftDrag(_OnDrag, null, _OnFinishDrag);

            m_InstantAnim = this.Resources["InstantShowAnim"] as Storyboard;

            this.DataContextChanged += _DataContextChangedEventHandler;
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);

            foreach (UIConnector c in m_uiConnectors.Values)
            {
                c.ResetPos();
            }
        }

        void _DataContextChangedEventHandler(object sender, DependencyPropertyChangedEventArgs e)
        {
            Renderer = DataContext as FSMStateRenderer;

            Node = Renderer.FSMStateOwner;

            //SetCanvas(Node.Renderer.RenderCanvas);

            _CreateConnectors();
            _BuildConnectionBinding();
            _SetCommentPos();
            if (DebugMgr.Instance.bBreaked)
                SetDebug(Node.Renderer.RunState);
            else
                SetDebug(NodeState.NS_INVALID);
        }

        private void _BuildConnectionBinding()
        {
            foreach (Connector ctr in Node.Conns.ConnectorsList)
            {
                _BuildConnectionBinding(ctr);
            }

            if (Node.Conns.ParentConnector != null)
                _BuildConnectionBinding(Node.Conns.ParentConnector);
        }

        private void _BuildConnectionBinding(Connector ctr, string identifier = null)
        {
            if (identifier == null)
                identifier = ctr.Identifier;

            if (m_uiConnectors.TryGetValue(identifier, out UIConnector uiConnector))
            {
                ConnectorGeometry geo = Node.Conns.GetConnector(identifier).Geo;
                uiConnector.DataContext = geo;
                //uiConnector.SetBinding(UIConnector.HotspotProperty, new Binding()
                //{
                //    Path = new PropertyPath("Pos"),
                //    Mode = BindingMode.OneWayToSource
                //});
            }
        }
        private void _CreateConnectors()
        {
            m_uiConnectors.Clear();
            connectors.Children.Clear();

            if (Node.Conns.ParentConnector != null)
            {
                FSMUIInConnector uiConnector = new FSMUIInConnector
                {
                    Ctr = Node.Conns.ParentConnector
                };

                connectors.Children.Add(uiConnector);

                m_uiConnectors.Add(Connector.IdentifierParent, uiConnector);
            }

            foreach (Connector ctr in Node.Conns.ConnectorsList)
            {
                //if (ctr is ConnectorNone)
                //    continue;

                FSMUIOutConnector uiConnector = new FSMUIOutConnector
                {
                    Ctr = ctr
                };

                connectors.Children.Add(uiConnector);

                m_uiConnectors.Add(ctr.Identifier, uiConnector);
            }
        }

        private void _SetCommentPos()
        {
            //if (bottomConnectors.Children.Count > 0)
            //{
            //    DockPanel.SetDock(commentBorder, Dock.Right);
            //    commentBorder.Margin = new Thickness(0, this.topConnectors.Height, 0, bottomConnectors.Height);
            //}
            //else
            {
                DockPanel.SetDock(commentBorder, Dock.Bottom);
                commentBorder.Margin = new Thickness(0);
            }
        }

        public static readonly DependencyProperty DebugTriggerProperty =
            DependencyProperty.Register("DebugTrigger",
            typeof(bool), typeof(UIFSMState), new FrameworkPropertyMetadata(DebugTrigger_PropertyChanged));
        private static void DebugTrigger_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            UINode c = (UINode)d;
            if (DebugMgr.Instance.bBreaked)
                c.SetDebug(c.Node.Renderer.RunState);
            else
                c.SetDebugInstant(c.Node.Renderer.RunState);
        }
        public bool DebugTrigger
        {
            get { return (bool)GetValue(DebugTriggerProperty); }
            set { SetValue(DebugTriggerProperty, value); }
        }


        Storyboard m_InstantAnim;

        public void SetDebugInstant(NodeState state = NodeState.NS_INVALID)
        {
            this.debugCover.Visibility = Visibility.Collapsed;
            if (state == NodeState.NS_INVALID)
            {
                m_InstantAnim.Remove(debugCover);
            }
            else
            {
                Brush bgBrush;
                switch (state)
                {
                    case NodeState.NS_SUCCESS:
                        bgBrush = new SolidColorBrush(Colors.LightGreen);
                        break;
                    case NodeState.NS_FAILURE:
                        bgBrush = new SolidColorBrush(Colors.LightBlue);
                        break;
                    case NodeState.NS_RUNNING:
                        bgBrush = new SolidColorBrush(Colors.LightPink);
                        break;
                    case NodeState.NS_BREAK:
                        bgBrush = new SolidColorBrush(Colors.DarkRed);
                        break;
                    default:
                        bgBrush = new SolidColorBrush(Colors.Red);
                        break;
                }
                this.debugCover.Background = bgBrush;

                m_InstantAnim.Begin(this.debugCover, true);
            }
        }

        public void SetDebug(NodeState state = NodeState.NS_INVALID)
        {
            m_InstantAnim.Remove(debugCover);
            if (state == NodeState.NS_INVALID)
            {
                this.debugCover.Visibility = Visibility.Collapsed;
            }
            else
            {
                Brush bgBrush;
                switch (state)
                {
                    case NodeState.NS_SUCCESS:
                        bgBrush = new SolidColorBrush(Colors.LightGreen);
                        break;
                    case NodeState.NS_FAILURE:
                        bgBrush = new SolidColorBrush(Colors.LightBlue);
                        break;
                    case NodeState.NS_RUNNING:
                        bgBrush = new SolidColorBrush(Colors.LightPink);
                        break;
                    case NodeState.NS_BREAK:
                        bgBrush = new SolidColorBrush(Colors.DarkRed);
                        break;
                    default:
                        bgBrush = new SolidColorBrush(Colors.Red);
                        break;
                }
                this.debugCover.Background = bgBrush;

                this.debugCover.Visibility = Visibility.Visible;
            }
        }

        void _OnClick()
        {
            m_Operation.MakeCanvasFocused();

            SelectHandler(this, true);
        }

        void _OnDrag(Vector delta, Point pos)
        {
            if (DebugMgr.Instance.IsDebugging())
                return;
            if (Node != null)
            {
                Node.Renderer.DragMain(delta, 1);
            }
        }

        void _OnFinishDrag(Vector delta, Point pos)
        {
            if (DebugMgr.Instance.IsDebugging() || Node == null)
                return;

            Node.Renderer.FinishDrag(delta, pos);
        }

        public void SetSelect(bool bSelect)
        {
            if (bSelect)
            {
                this.selectCover.Visibility = Visibility.Visible;
            }
            else
                this.selectCover.Visibility = Visibility.Collapsed;
        }

        public void OnDelete(int param)
        {
            Renderer.Delete(param);
        }

        public void OnDuplicated(int param)
        {
            if (Node.Type != FSMStateType.Normal)
                return;

            WorkBenchMgr.Instance.CloneTreeNodeToBench(Node, param != 0);
        }

        public void OnCopied(int param)
        {
            if (Node.Type != FSMStateType.Normal)
                return;

            WorkBenchMgr.Instance.CopyNode(Node, param != 0);
        }

        public void ToggleBreakPoint()
        {
            if (Node.DebugPointInfo.HitCount > 0)
                Node.SetDebugPoint(0);
            else
                Node.SetDebugPoint(1);
        }

        public void ToggleLogPoint()
        {
            if (Node.DebugPointInfo.HitCount < 0)
                Node.SetDebugPoint(0);
            else
                Node.SetDebugPoint(-1);
        }

        public void ToggleDisable()
        {
            Renderer.ToggleDisabled();
        }
    }
}
