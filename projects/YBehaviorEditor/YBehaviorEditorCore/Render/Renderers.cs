using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace YBehavior.Editor.Core
{
    public class ConnectorGeometry
    {
        public string Identifier { get; set; }
        public ConnectionHolder Holder { get; set; }
        Point m_Pos;
        public Point Pos
        {
            get { return m_Pos; }
            set
            {
                if (m_Pos == value)
                    return;
                m_Pos = value;

                if (onPosChanged != null)
                    onPosChanged();
            }
        }

        double m_MidY;
        public double MidY
        {
            get { return m_MidY; }
            set
            {
                if (m_MidY == value)
                    return;

                m_MidY = value;
                if (onMidYChanged != null)
                    onMidYChanged();
            }
        }
        public Action onPosChanged;
        public Action onMidYChanged;
    }

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

    public class Renderer : System.ComponentModel.INotifyPropertyChanged
    {
        Node m_Owner;
        public Node Owner { get { return m_Owner; } }

        public Geometry Geo { get; } = new Geometry();

        public Renderer(Node node)
        {
            m_Owner = node;
        }

        public void RefreshDebug()
        {
            DebugTrigger = !DebugTrigger;

            foreach (Node child in m_Owner.Conns)
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
            if (m_Owner.Parent != null)
            {
                ///> let the parent node sort the chilren
                m_Owner.Parent.OnChildChanged();
            }

            NodeMovedArg arg = new NodeMovedArg
            {
                Node = this.m_Owner
            };
            EventMgr.Instance.Send(arg);

            MoveNodeCommand moveNodeCommand = new MoveNodeCommand()
            {
                Node = this.m_Owner,
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

            foreach (Node child in m_Owner.Conns)
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
