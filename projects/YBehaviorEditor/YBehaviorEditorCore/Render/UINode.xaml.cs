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

namespace YBehavior.Editor.Core
{
    /// <summary>
    /// BehaviorNode.xaml 的交互逻辑
    /// </summary>
    public partial class UINode : UserControl, ISelectable, IDeletable, IDuplicatable, IBreakPointable
    {
        static SelectionStateChangeHandler defaultSelectHandler = new SelectionStateChangeHandler(SelectionMgr.Instance.OnSingleSelectedChange);

        Brush normalBorderBrush;
        public SelectionStateChangeHandler SelectHandler { get; set; }

        public Node Node { get; set; }

        Operation m_Operation;

        RenderCanvas m_Canvas;

        public UINode()
        {
            InitializeComponent();
            normalBorderBrush = this.border.BorderBrush;

            SelectHandler = new SelectionStateChangeHandler(defaultSelectHandler);

            m_Operation = new Operation(this.border);
            m_Operation.RegisterClick(_OnClick);
            m_Operation.RegisterDrag(_OnDrag);

            m_InstantAnim = this.Resources["InstantShowAnim"] as Storyboard;
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
                        LogMgr.Instance.Log("BREAK Instant ");
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
                        LogMgr.Instance.Log("BREAK");
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
            if (Node != null)
                Node.Renderer.DragMain(delta, pos);
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
            ///> Check if is root
            if (Node.Type == NodeType.NT_Root)
                return;

            ///> Disconnect all the connection
            NodesDisconnectedArg arg = new NodesDisconnectedArg();
            arg.ChildHolder = Node.Conns.ParentHolder;
            EventMgr.Instance.Send(arg);

            foreach (var child in Node.Conns)
            {
                Node chi = child as Node;
                if (chi == null)
                    continue;
                arg.ChildHolder = chi.Conns.ParentHolder;
                EventMgr.Instance.Send(arg);

                if (param != 0)
                    chi.Renderer.Frame.OnDelete(param);
            }

            m_Canvas.Panel.Children.Remove(this);

            RemoveNodeArg removeArg = new RemoveNodeArg();
            removeArg.Node = Node;
            EventMgr.Instance.Send(arg);
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
            if (Node.BreakPointInfo.HitCount > 0)
                Node.SetBreakPoint(0);
            else
                Node.SetBreakPoint(1);
        }
    }
}
