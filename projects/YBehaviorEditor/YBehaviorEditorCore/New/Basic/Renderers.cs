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
    /// <summary>
    /// ViewModel of a line
    /// </summary>
    public class ConnectionRenderer : System.ComponentModel.INotifyPropertyChanged
    {
        public ConnectionRenderer()
        { }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="isVertical">Connector.IsVertical</param>
        public ConnectionRenderer(bool isVertical)
        {
            m_bIsVertical = isVertical;
        }
        bool m_bIsVertical = true;
        bool m_bIsValid = true;
        public bool IsValid { get { return m_bIsValid; } }
        /// <summary>
        /// Model
        /// </summary>
        public Connection Owner { get; set; }
        /// <summary>
        /// Position of Start
        /// </summary>
        public Point ParentPos { get { return ParentConnectorGeo.Pos; } }
        /// <summary>
        /// Position of End
        /// </summary>
        public Point ChildPos { get { return ChildConnectorGeo.Pos; } }
        /// <summary>
        /// Position of the first corner
        /// </summary>
        public Point FirstCorner 
        {
            get
            {
                return m_bIsVertical ?
                    new Point(ParentConnectorGeo.Pos.X, m_ParentConnectorGeo.MidY) :
                    new Point(ParentConnectorGeo.Pos.X + 20, ParentConnectorGeo.Pos.Y);
            }
        }
        /// <summary>
        /// Position of the second corner
        /// </summary>
        public Point SecondCorner
        {
            get
            {
                return m_bIsVertical ?
                    new Point(ChildConnectorGeo.Pos.X, m_ParentConnectorGeo.MidY) :
                    new Point(ChildConnectorGeo.Pos.X - 20, ChildConnectorGeo.Pos.Y);
            }
        }
        /// <summary>
        /// Position of the note
        /// </summary>
        public Point NotePos { get { return new Point(SecondCorner.X + 5, SecondCorner.Y - 20); } }
        /// <summary>
        /// Note of the line
        /// </summary>
        public string Note { get { return Owner.Note; } }

        ConnectorGeometry m_ParentConnectorGeo;
        /// <summary>
        /// Position information of the Start
        /// </summary>
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
        /// <summary>
        /// Position information of the End
        /// </summary>
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

        //public void Destroy()
        //{
        //    ParentConnectorGeo = null;
        //    ChildConnectorGeo = null;
        //    m_bIsValid = false;
        //}

        /// <summary>
        /// Running state when debugging. Will return the state of End node
        /// </summary>
        public NodeState RunState { get { return DebugMgr.Instance.IsDebugging() ? DebugMgr.Instance.GetRunState(Owner.Ctr.To.Owner.UID, false) : NodeState.NS_INVALID; } }

        public event Action DebugEvent;
        /// <summary>
        /// Invoke the debug event
        /// </summary>
        public void RefreshDebug()
        {
            DebugEvent?.Invoke();
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
    /// <summary>
    /// Property used to refresh the view
    /// </summary>
    public enum RenderProperty
    {
        /// <summary>
        /// Nickname changed
        /// </summary>
        NickName,
        /// <summary>
        /// Comment changed
        /// </summary>
        Comment,
        /// <summary>
        /// UID changed
        /// </summary>
        UID,
        /// <summary>
        /// Disable state changed
        /// </summary>
        Disabled,
        /// <summary>
        /// Note chagned
        /// </summary>
        Note,
        /// <summary>
        /// Debug point state changed
        /// </summary>
        DebugPoint,
        /// <summary>
        /// Fold state changed
        /// </summary>
        Folded,
        /// <summary>
        /// ReturnType changed
        /// </summary>
        ReturnType,
        /// <summary>
        /// Condition of node changed
        /// </summary>
        Condition,
        /// <summary>
        /// Default state of fsm changed
        /// </summary>
        DefaultState,
    }
    /// <summary>
    /// Base class of ViewModel of node
    /// </summary>
    public class NodeBaseRenderer : System.ComponentModel.INotifyPropertyChanged
    {
        NodeBase m_Owner;
        /// <summary>
        /// Model
        /// </summary>
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
        /// <summary>
        /// Display name (Nickname or original name
        /// </summary>
        public virtual string FullName
        {
            get { return string.IsNullOrEmpty(Owner.NickName) ? Owner.Name : Owner.NickName; }
        }
        /// <summary>
        /// Nickname of node
        /// </summary>
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
        /// <summary>
        /// Toggle disabled state
        /// </summary>
        public void ToggleDisabled()
        {
            Disabled = !Owner.SelfDisabled;
        }
        /// <summary>
        /// Whether it's disabled
        /// </summary>
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
        /// <summary>
        /// When it's debugging, it's not editable
        /// </summary>
        public bool IsEditable
        {
            get { return !NetworkMgr.Instance.IsConnected; }
        }
        /// <summary>
        /// UID.FullName
        /// </summary>
        public string UITitle { get { return Owner.UID.ToString() + "." + FullName; } }
        public DebugPointInfo DebugPointInfo { get { return Owner.DebugPointInfo; } }
        /// <summary>
        /// Note of the node
        /// </summary>
        public string Note { get { return Owner.Note; } }
        /// <summary>
        /// Comment of the node
        /// </summary>
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

        public event Action DebugEvent;
        /// <summary>
        /// Invoke debug events
        /// </summary>
        public void RefreshDebug()
        {
            DebugEvent?.Invoke();
        }

        public event Action SelectEvent;
        /// <summary>
        /// Invoke select events
        /// </summary>
        public void SetSelect()
        {
            SelectEvent?.Invoke();
        }

        double m_CenterOffsetX = 0.0;
        public double CenterOffsetX
        {
            get { return m_CenterOffsetX; }
            set
            {
                m_CenterOffsetX = value;

                OnPropertyChanged("CenterOffsetX");
            }
        }
        double m_CenterOffsetY = 0.0;
        public double CenterOffsetY
        {
            get { return m_CenterOffsetY; }
            set
            {
                m_CenterOffsetY = value;

                OnPropertyChanged("CenterOffsetY");
            }
        }
        /// <summary>
        /// Running state of the node in debugging
        /// </summary>
        public NodeState RunState { get { return DebugMgr.Instance.IsDebugging() ? DebugMgr.Instance.GetRunState(m_Owner.UID, true) : NodeState.NS_INVALID; } }

        int m_DragParam = -1;
        /// <summary>
        /// Move the node
        /// </summary>
        /// <param name="delta">Delta position</param>
        /// <param name="param">if 0, move the children</param>
        public void DragMain(Vector delta, int param)
        {
            if (m_DragParam == -1)
                m_DragParam = param;
            Owner.Move(delta, m_DragParam);
        }
        /// <summary>
        /// Finish move the node
        /// </summary>
        /// <param name="delta">Delta position</param>
        /// <param name="pos">Final position</param>
        public void FinishDrag(Vector delta, Point pos)
        {
            Point finalPos = new Point(Math.Round(Owner.Geo.Pos.X / 10) * 10, Math.Round(Owner.Geo.Pos.Y / 10) * 10);
            Vector delta2 = finalPos - Owner.Geo.Pos;
            Owner.Move(delta2, m_DragParam);

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
        /// <summary>
        /// Set the position
        /// </summary>
        /// <param name="pos"></param>
        public void SetPos(Point pos)
        {
            Owner.Move(pos - Owner.Geo.Pos, 0);
        }
        /// <summary>
        /// Refresh view
        /// </summary>
        public void OnMove()
        {
            OnPropertyChanged("Owner");
        }

        protected virtual bool _BeforeDelete(int param) { return true; }
        /// <summary>
        /// Delete the node
        /// </summary>
        /// <param name="param">if 1, children will be deleted</param>
        public void Delete(int param)
        {
            if (!_BeforeDelete(param))
                return;

            ///> Disconnect all the connection
            //if (Owner.Conns.ParentConnector != null)
            //{
            //    while(Owner.Conns.ParentConnector.Conns.Count > 0)
            //    {
            //        WorkBenchMgr.Instance.DisconnectNodes(Owner.Conns.ParentConnector.Conns[0].Ctr);
            //    }
            //}

            foreach (Connector ctr in Owner.Conns.AllConnectors)
            {
                while (ctr.Conns.Count > 0)
                {
                    Connection conn = ctr.Conns[ctr.Conns.Count - 1];
                    WorkBenchMgr.Instance.DisconnectNodes(conn.Ctr);

                    if (ctr.GetPosType == Connector.PosType.CHILDREN && param != 0)
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
    /// <summary>
    /// ViewModel of tree node
    /// </summary>
    public class TreeNodeRenderer : NodeBaseRenderer
    {
        TreeNode m_TreeOwner;
        /// <summary>
        /// Model
        /// </summary>
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
        /// <summary>
        /// ReturnType of tree node
        /// </summary>
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
        /// <summary>
        /// Fold state of tree node
        /// </summary>
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
        /// <summary>
        /// Whether the condition connector is enabled
        /// </summary>
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
    /// <summary>
    /// ViewModel of connector
    /// </summary>
    public class ConnectorRenderer : System.ComponentModel.INotifyPropertyChanged
    {
        /// <summary>
        /// Model
        /// </summary>
        public Connector Owner { get; private set; }

        public string Identifier { get { return Owner.Identifier; } }

        public bool IsVisible => Owner.IsVisible;

        public ConnectorRenderer(Connector owner)
        {
            Owner = owner;
            Owner.IsVisibleEvent += Owner_IsVisibleEvent;
        }

        private void Owner_IsVisibleEvent()
        {
            OnPropertyChanged("IsVisible");
        }

        #region PropertyChanged
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        internal protected void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion
    }
}
