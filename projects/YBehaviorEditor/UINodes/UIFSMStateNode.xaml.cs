using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using YBehavior.Editor.Core.New;

namespace YBehavior.Editor
{
    public abstract class UIFSMStateNodeBase : UINodeBase<FSMStateNode, FSMStateRenderer>
    {
    }

    /// <summary>
    /// BehaviorNode.xaml 的交互逻辑
    /// </summary>
    public partial class UIFSMStateNode : UIFSMStateNodeBase
    {
        public override FrameworkElement SelectCoverUI { get { return this.selectCover;} }
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

        public UIFSMStateNode()
        {
            InitializeComponent();
            _Init();
        }

        protected override void _OnDataContextChanged()
        {
            _CreateConnectors();
        }

        private void _CreateConnectors()
        {
            m_uiConnectors.Clear();
            this.connectors.Children.Clear();

            if (Node.Conns.ParentConnector != null)
            {
                FSMUIInConnector uiConnector = new FSMUIInConnector()
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

                FSMUIOutConnector uiConnector = new FSMUIOutConnector(!(Node is FSMUpperStateNode)) ///> TODO: make this more elegant...
                {
                    Ctr = ctr
                };

                connectors.Children.Add(uiConnector);

                m_uiConnectors.Add(ctr.Identifier, uiConnector);
            }
        }
    }

    public class UIFSMUserStateNode : UIFSMStateNode, ISelectable, IDeletable, IDuplicatable, IDebugPointable, ICanDisable, ICanMakeDefault
    {
        public void MakeDefault()
        {
            if (WorkBenchMgr.Instance.ActiveWorkBench is FSMBench)
            {
                (WorkBenchMgr.Instance.ActiveWorkBench as FSMBench).SetDefault(Node);
            }
        }
    }

    public class UIFSMMetaStateNode : UIFSMUserStateNode
    {
        public UIFSMMetaStateNode()
        {
            m_Operation.RegisterLeftDoubleClick(_OnDoubleClick);
        }

        void _OnDoubleClick()
        {
            m_Operation.MakeCanvasFocused();

            WorkBenchMgr.Instance.AddRenderers((Node as FSMMetaStateNode).SubMachine, true, false);
        }
    }
    public class UIFSMSpecialStateNode : UIFSMStateNode, ISelectable, IDebugPointable
    {

    }

    public class UIFSMSpecialVirtualStateNode : UIFSMStateNode, ISelectable
    {

    }
}
