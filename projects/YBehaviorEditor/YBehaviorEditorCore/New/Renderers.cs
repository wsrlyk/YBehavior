using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace YBehavior.Editor.Core.New
{
    public class ConnectionRenderer : System.ComponentModel.INotifyPropertyChanged
    {
        bool m_bIsValid = true;
        public bool IsValid { get { return m_bIsValid; } }
        public Connection Owner { get; set; }
        public Point ParentPos { get { return ParentConnectorGeo.Pos; } }
        public Point ChildPos { get { return ChildConnectorGeo.Pos; } }
        public Point FirstCorner { get { return new Point(ParentConnectorGeo.Pos.X, m_ParentConnectorGeo.MidY); } }
        public Point SecondCorner { get { return new Point(ChildConnectorGeo.Pos.X, m_ParentConnectorGeo.MidY); } }
        public Point NotePos { get { return new Point(SecondCorner.X + 5, SecondCorner.Y - 20); } }
        public string Note { get { return Owner.Note; } }

        ConnectorGeometry m_ParentConnectorGeo;
        public ConnectorGeometry ParentConnectorGeo
        {
            get { return m_ParentConnectorGeo; }
            set
            {
                if (m_ParentConnectorGeo != null)
                {
                    m_ParentConnectorGeo.onPosChanged -= _OnParentPosChanged;
                    m_ParentConnectorGeo.onMidYChanged -= _OnMidYChanged;
                }
                m_ParentConnectorGeo = value;
                if (m_ParentConnectorGeo != null)
                {
                    m_ParentConnectorGeo.onPosChanged += _OnParentPosChanged;
                    m_ParentConnectorGeo.onMidYChanged += _OnMidYChanged;
                }
            }
        }
        ConnectorGeometry m_ChildConnectorGeo;
        public ConnectorGeometry ChildConnectorGeo
        {
            get { return m_ChildConnectorGeo; }
            set
            {
                if (m_ChildConnectorGeo != null)
                    m_ChildConnectorGeo.onPosChanged -= _OnChildPosChanged;
                m_ChildConnectorGeo = value;
                if (m_ChildConnectorGeo != null)
                    m_ChildConnectorGeo.onPosChanged += _OnChildPosChanged;
            }
        }

        void _OnChildPosChanged()
        {
            OnPropertyChanged("SecondCorner");
            OnPropertyChanged("ChildPos");
            OnPropertyChanged("NotePos");
        }

        void _OnParentPosChanged()
        {
            OnPropertyChanged("ParentPos");
            OnPropertyChanged("FirstCorner");
        }

        void _OnMidYChanged()
        {
            OnPropertyChanged("FirstCorner");
            OnPropertyChanged("SecondCorner");
            OnPropertyChanged("NotePos");
        }

        public void Destroy()
        {
            ParentConnectorGeo = null;
            ChildConnectorGeo = null;
            m_bIsValid = false;
        }

        public NodeState RunState { get { return DebugMgr.Instance.IsDebugging() ? DebugMgr.Instance.GetRunState(Owner.Ctr.To.Owner.UID, false) : NodeState.NS_INVALID; } }

        public void RefreshDebug()
        {
            DebugTrigger = !DebugTrigger;
        }

        /// <summary>
        /// Only a trigger to UI, meaningless
        /// </summary>
        private bool m_bDebugTrigger;
        public bool DebugTrigger
        {
            get { return m_bDebugTrigger; }
            set
            {
                m_bDebugTrigger = value;
                OnPropertyChanged("DebugTrigger");
            }
        }

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        internal protected void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
            }
        }
    }
    public enum RenderProperty
    {
        NickName,
        Comment,
        UID,
        Disabled,
        Note,
        DebugPoint,

        Folded,
        ReturnType,
        Condition,

        DefaultState,
    }

    public class NodeBaseRenderer : System.ComponentModel.INotifyPropertyChanged
    {
        NodeBase m_Owner;
        public NodeBase Owner { get { return m_Owner; } }

        public NodeBaseRenderer(NodeBase node)
        {
            m_Owner = node;
        }

        public void PropertyChange(RenderProperty property)
        {
            switch (property)
            {
                case RenderProperty.NickName:
                case RenderProperty.UID:
                    OnPropertyChanged("UITitle");
                    break;
                case RenderProperty.Disabled:
                    OnPropertyChanged("Disabled");
                    break;
                case RenderProperty.DebugPoint:
                    OnPropertyChanged("DebugPointInfo");
                    break;
                case RenderProperty.Note:
                    OnPropertyChanged("Note");
                    break;
                case RenderProperty.Comment:
                    OnPropertyChanged("Comment");
                    break;
                case RenderProperty.Folded:
                    OnPropertyChanged("Folded");
                    break;
                case RenderProperty.ReturnType:
                    OnPropertyChanged("ReturnType");
                    break;
                case RenderProperty.Condition:
                    OnPropertyChanged("EnableCondition");
                    break;
                case RenderProperty.DefaultState:
                    OnPropertyChanged("IsDefaultState");
                    break;
                default:
                    break;
            }
        }

        public virtual string FullName
        {
            get { return string.IsNullOrEmpty(Owner.NickName) ? Owner.Name : Owner.NickName; }
        }
        public string NickName
        {
            get { return Owner.NickName; }
            set
            {
                ChangeNodeNickNameCommand command = new ChangeNodeNickNameCommand()
                {
                    NodeRenderer = this,
                    OriginNickName = Owner.NickName,
                    FinalNickName = value,
                };

                Owner.NickName = value;

                WorkBenchMgr.Instance.PushCommand(command);
            }
        }

        public void ToggleDisabled()
        {
            Disabled = !Owner.SelfDisabled;
        }
        public bool Disabled
        {
            get { return Owner.Disabled; }
            set
            {
                if (Owner.SelfDisabled == value)
                    return;

                ChangeNodeDisableCommand command = new ChangeNodeDisableCommand()
                {
                    NodeRenderer = this,
                    OriginState = Owner.SelfDisabled,
                };

                Owner.Disabled = value;
                WorkBenchMgr.Instance.PushCommand(command);
            }
        }

        public string UITitle { get { return Owner.UID.ToString() + "." + FullName; } }
        public DebugPointInfo DebugPointInfo { get { return Owner.DebugPointInfo; } }
        public string Note { get { return Owner.Note; } }

        public string Comment
        {
            get { return Owner.Comment; }
            set
            {
                ChangeNodeCommentCommand command = new ChangeNodeCommentCommand()
                {
                    NodeRenderer = this,
                    OriginComment = Owner.Comment,
                    FinalComment = value,
                };

                Owner.Comment = value;

                WorkBenchMgr.Instance.PushCommand(command);
            }
        }

        public void RefreshDebug()
        {
            DebugTrigger = !DebugTrigger;

            //foreach (NodeBase child in m_Owner.Conns)
            //{
            //    child.Renderer.RefreshDebug();
            //}
        }

        /// <summary>
        /// Only a trigger to UI, meaningless
        /// </summary>
        private bool m_bDebugTrigger;
        public bool DebugTrigger
        {
            get { return m_bDebugTrigger; }
            set
            {
                m_bDebugTrigger = value;
                OnPropertyChanged("DebugTrigger");
            }
        }
        private bool m_bSelectTrigger;
        public bool SelectTrigger
        {
            get { return m_bSelectTrigger; }
            set
            {
                m_bSelectTrigger = value;
                OnPropertyChanged("SelectTrigger");
            }
        }

        public NodeState RunState { get { return DebugMgr.Instance.IsDebugging() ? DebugMgr.Instance.GetRunState(m_Owner.UID, true) : NodeState.NS_INVALID; } }

        int m_DragParam = -1;
        public void DragMain(Vector delta, int param)
        {
            if (m_DragParam == -1)
                m_DragParam = param;
            _Move(delta, m_DragParam);
        }

        public void FinishDrag(Vector delta, Point pos)
        {
            Point finalPos = new Point((int)(Owner.Geo.Pos.X / 10) * 10, (int)(Owner.Geo.Pos.Y / 10) * 10);
            Vector delta2 = finalPos - Owner.Geo.Pos;
            _Move(delta2, 0);

            if (m_Owner.Conns.ParentConnector != null)
            {
                ///> let the parent node sort the chilren
                foreach (var conn in m_Owner.Conns.ParentConnector.Conns)
                {
                    conn.Ctr.From.Owner.OnConnectToChanged();
                }
            }

            NodeMovedArg arg = new NodeMovedArg
            {
                Node = this.m_Owner
            };
            EventMgr.Instance.Send(arg);

            MoveNodeCommand moveNodeCommand = new MoveNodeCommand()
            {
                NodeRenderer = this,
                OriginPos = finalPos - delta - delta2,
                FinalPos = finalPos,
                DragParam = m_DragParam,
            };
            WorkBenchMgr.Instance.PushCommand(moveNodeCommand);
            m_DragParam = -1;
        }
        public void SetPos(Point pos)
        {
            _Move(pos - Owner.Geo.Pos, 0);
        }

        void _Move(Vector delta, int param)
        {
            Owner.Geo.Pos = Owner.Geo.Pos + delta;

            OnPropertyChanged("Owner");

            if (param == 0)
            {
                foreach (NodeBase child in m_Owner.Conns)
                {
                    child.Renderer._Move(delta, param);
                }
            }
        }

        protected virtual bool _BeforeDelete(int param) { return true; }
        public void Delete(int param)
        {
            if (!_BeforeDelete(param))
                return;

            ///> Disconnect all the connection
            if (Owner.Conns.ParentConnector != null)
            {
                while(Owner.Conns.ParentConnector.Conns.Count > 0)
                {
                    WorkBenchMgr.Instance.DisconnectNodes(Owner.Conns.ParentConnector.Conns[0].Ctr);
                }
            }

            foreach (Connector ctr in Owner.Conns.ConnectorsList)
            {
                while (ctr.Conns.Count > 0)
                {
                    Connection conn = ctr.Conns[ctr.Conns.Count - 1];
                    WorkBenchMgr.Instance.DisconnectNodes(conn.Ctr);

                    if (param != 0)
                        conn.Ctr.To.Owner.Renderer.Delete(param);
                }
            }

            WorkBenchMgr.Instance.RemoveNode(Owner);
        }

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        internal protected void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
            }
        }
    }

    public class TreeNodeRenderer : NodeBaseRenderer
    {
        TreeNode m_TreeOwner;
        public TreeNode TreeOwner { get { return m_TreeOwner; } }

        public TreeNodeRenderer(TreeNode treeNode) : base(treeNode)
        {
            m_TreeOwner = treeNode;
        }

        protected override bool _BeforeDelete(int param)
        {
            ///> Check if is root
            if ((Owner as TreeNode).Type == TreeNodeType.TNT_Root)
                return false;

            ///> If folded but remove only this, unfold first
            if (Folded && param == 0)
                Folded = !Folded;

            return true;
        }

        public string ReturnType
        {
            get { return TreeOwner.ReturnType; }
            set
            {
                ChangeTreeNodeReturnTypeCommand command = new ChangeTreeNodeReturnTypeCommand()
                {
                    NodeRenderer = this,
                    OriginReturnType = TreeOwner.ReturnType,
                    FinalReturnType = value,
                };

                TreeOwner.ReturnType = value;

                OnPropertyChanged("ReturnType");

                WorkBenchMgr.Instance.PushCommand(command);
            }
        }

        public bool Folded
        {
            get { return TreeOwner.Folded; }
            set
            {
                if (TreeOwner.Folded != value)
                {
                    TreeOwner.Folded = value;

                    if (value)
                        WorkBenchMgr.Instance.RemoveRenderers(TreeOwner, true);
                    else
                        WorkBenchMgr.Instance.AddRenderers(TreeOwner, false, true);

                    OnPropertyChanged("Folded");
                }

            }
        }

        public bool EnableCondition
        {
            get { return TreeOwner.EnableCondition; }
            set
            {
                if (value == false)
                {
                    ///> Check if has connection
                    if (TreeOwner.HasConditionConnection)
                    {
                        ShowSystemTipsArg showSystemTipsArg = new ShowSystemTipsArg()
                        {
                            Content = "Should remove connection first.",
                            TipType = ShowSystemTipsArg.TipsType.TT_Error,
                        };
                        EventMgr.Instance.Send(showSystemTipsArg);
                        return;
                    }
                }
                TreeOwner.EnableCondition = value;
                OnPropertyChanged("EnableCondition");
            }
        }
    }
}
