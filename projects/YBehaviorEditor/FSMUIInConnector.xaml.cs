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
using YBehavior.Editor.Core.New;

namespace YBehavior.Editor
{
    /// <summary>
    /// UIConnector.xaml 的交互逻辑
    /// </summary>
    public partial class FSMUIInConnector : UIConnector, IDropable
    {
        Brush normalBorderBrush;

        public override Border Main { get { return this.main; } }

        Vector m_RelativePos = new Vector(double.MaxValue, double.MaxValue);

        public FSMUIInConnector()
        {
            InitializeComponent();

            normalBorderBrush = this.Main.BorderBrush;

            DropHandler = new DropHandler(defaultDropHandler);

            //m_Operation.RegisterRightDrag(_OnDragged, _OnStartDragged, _OnFinishDragged);
        }

        void _OnLayoutUpdated(object sender, EventArgs e)
        {
            _UpdateHotspot();
        }

        private void _UpdateHotspot()
        {
            if (OwnerNode != null)
            {
                if (m_RelativePos.X == double.MaxValue && m_RelativePos.Y == double.MaxValue)
                {
                    m_RelativePos = TransformToAncestor(OwnerNode).Transform(new Point(ActualWidth / 2, ActualHeight / 2)) - new Point();
                }

                if (OwnerNode.DataContext is NodeBaseRenderer)
                {
                    Point pos = (OwnerNode.DataContext as NodeBaseRenderer).Owner.Geo.Pos + m_RelativePos;
                    //Hotspot = pos;
                    (this.DataContext as ConnectorGeometry).Pos = pos;
                }
            }

            //Hotspot = GetPos(Ancestor);
        }

        public void SetDropped(bool bDropped)
        {
            if (bDropped)
                this.Main.BorderBrush = new SolidColorBrush(Colors.Chocolate);
            else
                this.Main.BorderBrush = normalBorderBrush;
        }


        public void OnDropped(IDragable dragable)
        {
            if (DebugMgr.Instance.IsDebugging())
                return;

            if (dragable == null)
                return;

            FSMUIOutConnector other = dragable as FSMUIOutConnector;
            if (other == null)
                return;

            if (this.Ctr.Owner is FSMMetaStateNode || this.Ctr.Owner is FSMUpperStateNode)
            {
                MenuItemViewModel menuModel = PopMenuUtility.CreateFSMConnectionDropMenu(other.Ctr.Owner as FSMStateNode, this.Ctr.Owner as FSMStateNode);
                PopMenuArg arg = new PopMenuArg
                {
                    MenuModel = menuModel
                };
                EventMgr.Instance.Send(arg);
            }
            else
                WorkBenchMgr.Instance.ConnectNodes(other.Ctr, this.Ctr);
        }
    }
}
