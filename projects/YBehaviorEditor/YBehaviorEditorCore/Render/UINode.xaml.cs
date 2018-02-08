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

namespace YBehavior.Editor.Core
{
    /// <summary>
    /// BehaviorNode.xaml 的交互逻辑
    /// </summary>
    public partial class UINode : UserControl, ISelectable, IDeletable
    {
        static SelectionStateChangeHandler defaultSelectHandler = new SelectionStateChangeHandler(SelectionMgr.Instance.OnSingleSelectedChange);

        Brush normalBorderBrush;
        public SelectionStateChangeHandler SelectHandler { get; set; }
        
        public Node Node { get; set; }

        Operation m_Operation;
        Panel m_Panel;

        public UINode()
        {
            InitializeComponent();
            normalBorderBrush = this.border.BorderBrush;

            SelectHandler = new SelectionStateChangeHandler(defaultSelectHandler);

            m_Operation = new Operation(this.border);
            m_Operation.RegisterClick(_OnClick);
            m_Operation.RegisterDrag(_OnDrag);
        }

        public void SetCanvas(Panel panel)
        {
            m_Panel = panel;
            m_Operation.SetPanel(panel);
        }


        void _OnClick()
        {
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

        public void OnDelete()
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
            }

            m_Panel.Children.Remove(this);

            RemoveNodeArg removeArg = new RemoveNodeArg();
            removeArg.Node = Node;
            EventMgr.Instance.Send(arg);
        }
    }
}
