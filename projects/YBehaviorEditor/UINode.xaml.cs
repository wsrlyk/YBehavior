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
    public partial class UINode : UserControl, ISelectable, IDeletable, IDuplicatable, IDebugPointable
    {
        static SelectionStateChangeHandler defaultSelectHandler = SelectionMgr.Instance.OnSingleSelectedChange;

        Brush normalBorderBrush;
        public SelectionStateChangeHandler SelectHandler { get; set; }

        public Node Node { get; set; }

        Operation m_Operation;

        RenderCanvas m_Canvas;

        Dictionary<string, UIConnector> m_uiConnectors = new Dictionary<string, UIConnector>();

        public UINode()
        {
            InitializeComponent();
            normalBorderBrush = this.border.BorderBrush;

            SelectHandler = defaultSelectHandler;

            m_Operation = new Operation(this.border);
            m_Operation.RegisterClick(_OnClick);
            m_Operation.RegisterDrag(_OnDrag);

            m_InstantAnim = this.Resources["InstantShowAnim"] as Storyboard;

            this.DataContextChanged += _DataContextChangedEventHandler;
        }

        void _DataContextChangedEventHandler(object sender, DependencyPropertyChangedEventArgs e)
        {
            Node = (DataContext as Renderer).Owner;
            Node.Renderer.UINodeRef = this;

            SetCanvas(Node.Renderer.RenderCanvas);

            _CreateConnectors();
            _BuildConnectionBinding();
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
                ConnectorGeometry geo = Node.Renderer.GetConnectorGeometry(identifier);
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

            if (Node.Conns.ParentHolder != null)
            {
                UIConnector uiConnector = new UIConnector
                {
                    Title = Node.Icon,
                    ConnHolder = Node.Conns.ParentHolder
                };
                uiConnector.SetCanvas(m_Canvas);

                topConnectors.Children.Add(uiConnector);

                m_uiConnectors.Add(Connection.IdentifierParent, uiConnector);
            }

            foreach (ConnectionHolder conn in Node.Conns.ConnectionsList)
            {
                if (conn.Conn is ConnectionNone)
                    continue;

                UIConnector uiConnector = new UIConnector
                {
                    Title = conn.Conn.Identifier,
                    ConnHolder = conn
                };
                uiConnector.SetCanvas(m_Canvas);

                bottomConnectors.Children.Add(uiConnector);

                m_uiConnectors.Add(conn.Conn.Identifier, uiConnector);
            }
        }

        public void SetCanvas(RenderCanvas canvas)
        {
            m_Canvas = canvas;
            m_Operation.SetCanvas(canvas);
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

                //Storyboard board = this.Resources["ConstantShowAnim"] as Storyboard;
                //Storyboard.SetTargetName(board, "debugCover");
                //this.BeginStoryboard(board);
            }
        }

        void _OnClick()
        {
            if (Node is Tree)
                return;
            SelectHandler(this, true);

            m_Operation.MakeCanvasFocused();
        }

        void _OnDrag(Vector delta, Point pos)
        {
            if (DebugMgr.Instance.IsDebugging())
                return;
            if (Node != null)
            {
                Node.Renderer.DragMain(delta);

                ///> let the parent node sort the chilren
                if (delta.LengthSquared == 0 && Node.Parent != null)
                {
                    Node.Parent.OnChildChanged();

                    NodeMovedArg arg = new NodeMovedArg
                    {
                        Node = this.Node
                    };
                    EventMgr.Instance.Send(arg);
                }
            }
        }

        public void SetSelect(bool bSelect)
        {
            if (bSelect)
                this.border.BorderBrush = new SolidColorBrush(Colors.DarkBlue);
            else
                this.border.BorderBrush = normalBorderBrush;
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

            Node node = null;
            if ((node = WorkBenchMgr.Instance.CloneNodeToBench(Node, param != 0)) != null)
            {
                NewNodeAddedArg arg = new NewNodeAddedArg();
                arg.Node = node;
                EventMgr.Instance.Send(arg);
            }
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
    }
}
