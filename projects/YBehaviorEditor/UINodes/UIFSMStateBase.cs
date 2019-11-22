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
    public abstract class UIFSMStateBase : YUserControl, ISelectable
    {
        static protected SelectionStateChangeHandler defaultSelectHandler = SelectionMgr.Instance.OnSingleSelectedChange;

        public SelectionStateChangeHandler SelectHandler { get; set; }

        public FSMStateNode Node { get; set; }
        public FSMStateRenderer Renderer { get; set; }

        public abstract FrameworkElement SelectCoverUI { get; }
        public abstract Brush OutlookBrush { get; set; }
        public abstract Panel ConnectorsUI { get; }
        public abstract FrameworkElement CommentUI { get; }
        public abstract FrameworkElement DebugUI { get; }
        public abstract Brush DebugBrush { get; set; }

        protected Operation m_Operation;

        protected Dictionary<string, UIConnector> m_uiConnectors = new Dictionary<string, UIConnector>();

        public UIFSMStateBase()
        {
        }

        protected void _Init()
        {
            this.SelectCoverUI.Visibility = Visibility.Collapsed;

            SelectHandler = defaultSelectHandler;

            m_Operation = new Operation(this);
            m_Operation.RegisterLeftClick(_OnClick);
            m_Operation.RegisterLeftDrag(_OnDrag, null, _OnFinishDrag);

            m_InstantAnim = Application.Current.Resources["InstantShowAnim"] as Storyboard;

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
            _SetOutlook();
            _CreateConnectors();
            _BuildConnectionBinding();
            _SetCommentPos();
            if (DebugMgr.Instance.bBreaked)
                SetDebug(Node.Renderer.RunState);
            else
                SetDebug(NodeState.NS_INVALID);

            if (Node is FSMMetaStateNode)
                m_Operation.RegisterLeftDoubleClick(_OnDoubleClick);
        }

        private void _SetOutlook()
        {
            if (Node.Type == FSMStateType.User)
            {
                if (Node is FSMNormalStateNode)
                    this.OutlookBrush = (SolidColorBrush)this.FindResource("normalColor");
                else if (Node is FSMMetaStateNode)
                    this.OutlookBrush = (SolidColorBrush)this.FindResource("metaColor");
            }
            else if (Node.Type == FSMStateType.Special)
            {
                if (Node is FSMEntryStateNode)
                    this.OutlookBrush = (SolidColorBrush)this.FindResource("entryColor");
                else if (Node is FSMExitStateNode)
                    this.OutlookBrush = (SolidColorBrush)this.FindResource("exitColor");
                else if (Node is FSMAnyStateNode)
                    this.OutlookBrush = (SolidColorBrush)this.FindResource("anyColor");
                else if (Node is FSMUpperStateNode)
                    this.OutlookBrush = (SolidColorBrush)this.FindResource("upperColor");
            }
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
            ConnectorsUI.Children.Clear();

            if (Node.Conns.ParentConnector != null)
            {
                FSMUIInConnector uiConnector = new FSMUIInConnector()
                {
                    Ctr = Node.Conns.ParentConnector
                };

                ConnectorsUI.Children.Add(uiConnector);

                m_uiConnectors.Add(Connector.IdentifierParent, uiConnector);
            }

            foreach (Connector ctr in Node.Conns.ConnectorsList)
            {
                //if (ctr is ConnectorNone)
                //    continue;

                FSMUIOutConnector uiConnector = new FSMUIOutConnector(!(Node is FSMUpperStateNode)) ///> TODO: make this more elegant...
                {
                    Ctr = ctr
                };

                ConnectorsUI.Children.Add(uiConnector);

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
                DockPanel.SetDock(CommentUI, Dock.Bottom);
                CommentUI.Margin = new Thickness(0);
            }
        }

        public static readonly DependencyProperty DebugTriggerProperty =
            DependencyProperty.Register("DebugTrigger",
            typeof(bool), typeof(UIFSMStateBase), new FrameworkPropertyMetadata(DebugTrigger_PropertyChanged));
        private static void DebugTrigger_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            UIFSMStateBase c = (UIFSMStateBase)d;
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
            this.DebugUI.Visibility = Visibility.Collapsed;
            if (state == NodeState.NS_INVALID)
            {
                m_InstantAnim.Remove(DebugUI);
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
                this.DebugBrush = bgBrush;

                m_InstantAnim.Begin(this.DebugUI, true);
            }
        }

        public void SetDebug(NodeState state = NodeState.NS_INVALID)
        {
            m_InstantAnim.Remove(DebugUI);
            if (state == NodeState.NS_INVALID)
            {
                this.DebugUI.Visibility = Visibility.Collapsed;
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
                this.DebugBrush = bgBrush;

                this.DebugUI.Visibility = Visibility.Visible;
            }
        }
        void _OnDoubleClick()
        {
            m_Operation.MakeCanvasFocused();

            WorkBenchMgr.Instance.AddRenderers((Node as FSMMetaStateNode).SubMachine, true, false);
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
                this.SelectCoverUI.Visibility = Visibility.Visible;
            }
            else
                this.SelectCoverUI.Visibility = Visibility.Collapsed;
        }

        public void OnDelete(int param)
        {
            if (Node.Type != FSMStateType.User)
                return;
            Renderer.Delete(param);
        }

        public void OnDuplicated(int param)
        {
            if (Node.Type != FSMStateType.User)
                return;

            WorkBenchMgr.Instance.CloneTreeNodeToBench(Node, param != 0);
        }

        public void OnCopied(int param)
        {
            if (Node.Type != FSMStateType.User)
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

        public void MakeDefault()
        {
            if (WorkBenchMgr.Instance.ActiveWorkBench is FSMBench)
            {
                (WorkBenchMgr.Instance.ActiveWorkBench as FSMBench).SetDefault(Node);
            }
        }
    }
}
