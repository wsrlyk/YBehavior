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

        public Point ParentPos { get { return ParentConnectorGeo.Pos; } }
        public Point ChildPos { get { return ChildConnectorGeo.Pos; } }
        public Point FirstCorner { get { return new Point(ParentConnectorGeo.Pos.X, m_ParentConnectorGeo.MidY); } }
        public Point SecondCorner { get { return new Point(ChildConnectorGeo.Pos.X, m_ParentConnectorGeo.MidY); } }

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

        public ConnectionHolder ChildConn { get; set; }

        void _OnChildPosChanged()
        {
            OnPropertyChanged("SecondCorner");
            OnPropertyChanged("ChildPos");
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
        }

        public void Destroy()
        {
            ParentConnectorGeo = null;
            ChildConnectorGeo = null;
            ChildConn = null;
            m_bIsValid = false;
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
        Disable,
        Folded,
        ReturnType,
        Note,
    }

    public class NodeBaseRenderer : System.ComponentModel.INotifyPropertyChanged
    {
        NodeBase m_Owner;
        public NodeBase Owner { get { return m_Owner; } }

        public Geometry Geo { get; } = new Geometry();

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
                case RenderProperty.Disable:
                    OnPropertyChanged("Disable");
                    break;
                default:
                    break;
            }
        }

        public string FullName
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

            foreach (NodeBase child in m_Owner.Conns)
            {
                child.Renderer.RefreshDebug();
            }
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

        public NodeState RunState { get { return DebugMgr.Instance.IsDebugging() ? DebugMgr.Instance.GetRunState(m_Owner.UID) : NodeState.NS_INVALID; } }

        public void DragMain(Vector delta)
        {
            _Move(delta);
        }

        public void FinishDrag(Vector delta, Point pos)
        {
            if (m_Owner.Conns.ParentConnector != null)
            {
                ///> let the parent node sort the chilren
                foreach (var conn in m_Owner.Conns.ParentConnector.Conns)
                {
                    conn.From.Owner.OnConnectToChanged();
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
                OriginPos = pos - delta,
                FinalPos = pos
            };
            WorkBenchMgr.Instance.PushCommand(moveNodeCommand);
        }
        public void SetPos(Point pos)
        {
            _Move(pos - Geo.Pos);
        }

        void _Move(Vector delta)
        {
            Geo.Pos = Geo.Pos + delta;

            OnPropertyChanged("Geo");

            foreach (NodeBase child in m_Owner.Conns)
            {
                child.Renderer._Move(delta);
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
}
