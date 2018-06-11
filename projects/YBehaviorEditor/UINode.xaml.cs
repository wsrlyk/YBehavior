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
using YBehavior.Editor.Core;

namespace YBehavior.Editor
{
    /// <summary>
    /// BehaviorNode.xaml 的交互逻辑
    /// </summary>
    public partial class UINode : YUserControl, ISelectable, IDeletable, IDuplicatable, IDebugPointable, ICanDisable, IHasCondition
    {
        static SelectionStateChangeHandler defaultSelectHandler = SelectionMgr.Instance.OnSingleSelectedChange;

        public SelectionStateChangeHandler SelectHandler { get; set; }

        public Node Node { get; set; }

        Operation m_Operation;

        Dictionary<string, UIConnector> m_uiConnectors = new Dictionary<string, UIConnector>();

        public UINode()
        {
            InitializeComponent();
            this.selectCover.Visibility = Visibility.Collapsed;

            SelectHandler = defaultSelectHandler;

            m_Operation = new Operation(this);
            m_Operation.RegisterClick(_OnClick);
            m_Operation.RegisterDrag(_OnDrag, _OnFinishDrag);

            m_InstantAnim = this.Resources["InstantShowAnim"] as Storyboard;

            this.DataContextChanged += _DataContextChangedEventHandler;
        }

        void _DataContextChangedEventHandler(object sender, DependencyPropertyChangedEventArgs e)
        {
            Node = (DataContext as Renderer).Owner;

            //SetCanvas(Node.Renderer.RenderCanvas);

            _CreateConnectors();
            _BuildConnectionBinding();
            _SetCommentPos();
            SetDebug(NodeState.NS_INVALID);
        }

        private void _BuildConnectionBinding()
        {
            foreach (ConnectionHolder conn in Node.Conns.ConnectionsList)
            {
                _BuildConnectionBinding(conn);
            }

            if (Node.Conns.ParentHolder != null)
                _BuildConnectionBinding(Node.Conns.ParentHolder, Connection.IdentifierParent);
        }

        private void _BuildConnectionBinding(ConnectionHolder conn, string identifier = null)
        {
            if (identifier == null)
                identifier = conn.Conn.Identifier;

            if (m_uiConnectors.TryGetValue(identifier, out UIConnector uiConnector))
            {
                ConnectorGeometry geo = Node.Conns.GetConnHolder(identifier).Geo;
                uiConnector.DataContext = geo;
                uiConnector.SetBinding(UIConnector.HotspotProperty, new Binding()
                {
                    Path = new PropertyPath("Pos"),
                    Mode = BindingMode.OneWayToSource
                });
            }
        }
        private void _CreateConnectors()
        {
            m_uiConnectors.Clear();
            topConnectors.Children.Clear();
            bottomConnectors.Children.Clear();
            leftConnectors.Child = null;

            foreach (ConnectionHolder conn in Node.Conns.ConnectionsList)
            {
                if (conn.Conn is ConnectionNone)
                    continue;

                UIConnector uiConnector = new UIConnector
                {
                    Title = conn.Conn.Identifier,
                    ConnHolder = conn
                };
                //uiConnector.SetCanvas(m_Canvas);
                if (conn.Conn.Identifier == Connection.IdentifierCondition)
                {
                    leftConnectors.Child = uiConnector;
                }
                else
                    bottomConnectors.Children.Add(uiConnector);

                m_uiConnectors.Add(conn.Conn.Identifier, uiConnector);
            }

            if (Node.Conns.ParentHolder != null)
            {
                UIConnector uiConnector = new UIConnector
                {
                    Title = Node.Icon,
                    ConnHolder = Node.Conns.ParentHolder
                };
                //uiConnector.SetCanvas(m_Canvas);

                topConnectors.Children.Add(uiConnector);

                m_uiConnectors.Add(Connection.IdentifierParent, uiConnector);
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

        public static readonly DependencyProperty DebugInstantProperty =
            DependencyProperty.Register("DebugInstant",
            typeof(bool), typeof(UINode), new FrameworkPropertyMetadata(DebugInstant_PropertyChanged));
        private static void DebugInstant_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            UINode c = (UINode)d;
            c.SetDebugInstant(c.Node.Renderer.RunState);
        }
        public bool DebugInstant
        {
            get { return (bool)GetValue(DebugInstantProperty); }
            set { SetValue(DebugInstantProperty, value); }
        }

        public static readonly DependencyProperty DebugConstantProperty =
            DependencyProperty.Register("DebugConstant",
            typeof(bool), typeof(UINode), new FrameworkPropertyMetadata(DebugConstant_PropertyChanged));
        private static void DebugConstant_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            UINode c = (UINode)d;
            c.SetDebug(c.Node.Renderer.RunState);
        }
        public bool DebugConstant
        {
            get { return (bool)GetValue(DebugConstantProperty); }
            set { SetValue(DebugConstantProperty, value); }
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
                    case NodeState.NS_FAILED:
                        bgBrush = new SolidColorBrush(Colors.DarkSeaGreen);
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

                //                this.debugCover.Visibility = Visibility.Visible;
                m_InstantAnim.Begin(this.debugCover, true);
                //LogMgr.Instance.Log("InstantAnim");
                //this.debugCover.BeginStoryboard(m_InstantAnim, HandoffBehavior.SnapshotAndReplace, true);
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
                    case NodeState.NS_FAILED:
                        bgBrush = new SolidColorBrush(Colors.DarkSeaGreen);
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

                //LogMgr.Instance.Log("ConstantAnim");

                //Storyboard board = this.Resources["ConstantShowAnim"] as Storyboard;
                //Storyboard.SetTargetName(board, "debugCover");
                //this.BeginStoryboard(board);
            }
        }

        void _OnClick()
        {
            m_Operation.MakeCanvasFocused();
            if (Node is Tree)
                return;
            SelectHandler(this, true);
        }

        void _OnDrag(Vector delta, Point pos)
        {
            if (DebugMgr.Instance.IsDebugging())
                return;
            if (Node != null)
            {
                Node.Renderer.DragMain(delta);
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
                this.Node.Variables.RefreshVariables();
            }
            else
                this.selectCover.Visibility = Visibility.Collapsed;
        }

        public void OnDelete(int param)
        {
            Node.Delete(param);
        }

        public void OnDuplicated(int param)
        {
            ///> Check if is root
            if (Node.Type == NodeType.NT_Root)
                return;

            WorkBenchMgr.Instance.CloneNodeToBench(Node, param != 0);
        }

        public void OnCopied(int param)
        {
            ///> Check if is root
            if (Node.Type == NodeType.NT_Root)
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
            Node.Disabled = !Node.SelfDisabled;
        }

        public void ToggleCondition()
        {
            Node.EnableCondition = !Node.EnableCondition;
        }
    }
}
