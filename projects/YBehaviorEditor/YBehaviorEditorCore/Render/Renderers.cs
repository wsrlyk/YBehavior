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
    public class RenderCanvas
    {
        public FrameworkElement Panel { get; set; }
    }

    public class ConnectorGeometry
    {
        public string Identifier { get; set; }

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

        RenderCanvas m_Canvas = new RenderCanvas();
        public RenderCanvas RenderCanvas { get { return m_Canvas; } }

        //UINode m_uiFrame;
        //public UINode Frame { get { return m_uiFrame; } }

        public Geometry Geo { get; } = new Geometry();

        public Renderer(Node node)
        {
            m_Owner = node;
            //m_uiFrame = new UINode
            //{
            //    Node = node
            //};

            _CreateSelf();
        }

        public void RefreshDebug(bool bInstant)
        {
            if (bInstant)
                DebugInstant = !DebugInstant;
            else
                DebugConstant = !DebugConstant;

            foreach (Node child in m_Owner.Conns)
            {
                child.Renderer.RefreshDebug(bInstant);
            }
        }

        private bool m_bDebugInstant;
        public bool DebugInstant
        {
            get { return m_bDebugInstant; }
            set
            {
                m_bDebugInstant = value;
                OnPropertyChanged("DebugInstant");
            }
        }
        private bool m_bDebugConstant;
        public bool DebugConstant
        {
            get { return m_bDebugConstant; }
            set
            {
                m_bDebugConstant = value;
                OnPropertyChanged("DebugConstant");
            }
        }

        public NodeState RunState { get { return DebugMgr.Instance.IsDebugging() ? DebugMgr.Instance.GetRunState(m_Owner.UID) : NodeState.NS_INVALID; } }

        protected virtual void _CreateSelf()
        {
            //_CreateFrame(m_Owner);
            //_CreateConnectors();
            _SetCommentPos();
        }

        //private void _CreateFrame(Node node)
        //{
        //    m_uiFrame.SetCanvas(m_Canvas);
        //    m_uiFrame.DataContext = node;
        //}

        private void _SetCommentPos()
        {
            //if (m_uiFrame.bottomConnectors.Children.Count > 0)
            //    DockPanel.SetDock(m_uiFrame.commentBorder, Dock.Right);
            //else
            //    DockPanel.SetDock(m_uiFrame.commentBorder, Dock.Bottom);
        }
        public void DragMain(Vector delta)
        {
            _Move(delta);
        }

        public void SetPos(Point pos)
        {
            _Move(pos - Geo.Pos);
        }

        void _Move(Vector delta)
        {
            Geo.Pos = Geo.Pos + delta;

            OnPropertyChanged("Geo");
            // TODO
            //Canvas.SetLeft(m_uiFrame, Geo.Pos.X);
            //Canvas.SetTop(m_uiFrame, Geo.Pos.Y);

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
