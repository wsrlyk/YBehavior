﻿using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;
using YBehavior.Editor.Core.New;

namespace YBehavior.Editor
{
    /// <summary>
    /// UI of node
    /// </summary>
    public abstract class UINodeBase<NodeType, NodeRendererType>: YUserControl, IDebugControl where NodeRendererType: NodeBaseRenderer where NodeType : NodeBase
    {
        static protected SelectionStateChangeHandler defaultSelectHandler = SelectionMgr.Instance.OnSingleSelectedChange;

        public SelectionStateChangeHandler SelectHandler { get; set; }
        /// <summary>
        /// Model
        /// </summary>
        public NodeType Node { get; set; }
        /// <summary>
        /// ViewModel
        /// </summary>
        public NodeRendererType Renderer { get; set; }
        /// <summary>
        /// Showed when selected
        /// </summary>
        public abstract FrameworkElement SelectCoverUI { get; }
        /// <summary>
        /// Main color
        /// </summary>
        public abstract Brush OutlookBrush { get; set; }
        /// <summary>
        /// UI of comment
        /// </summary>
        public abstract FrameworkElement CommentUI { get; }
        /// <summary>
        /// Showed when it's in debugging
        /// </summary>
        public abstract FrameworkElement DebugUI { get; }
        /// <summary>
        /// Debug color
        /// </summary>
        public abstract Brush DebugBrush { get; set; }
        /// <summary>
        /// Get the debug running state
        /// </summary>
        public NodeState RunState => Node.Renderer.RunState;
        protected Operation m_Operation;
        protected DebugControl m_DebugControl;

        protected Dictionary<Connector, UIConnector> m_uiConnectors = new Dictionary<Connector, UIConnector>();

        public UINodeBase()
        {
        }

        protected void _Init()
        {
            this.SelectCoverUI.Visibility = Visibility.Collapsed;

            SelectHandler = defaultSelectHandler;

            m_Operation = new Operation(this);
            m_Operation.RegisterLeftClick(_OnClick);
            m_Operation.RegisterLeftDrag(_OnDrag, null, _OnFinishDrag);

            m_InstantAnim = Application.Current.Resources["InstantShowAnim"] as Storyboard;
            m_DebugControl = new DebugControl(this);

            this.DataContextChanged += _DataContextChangedEventHandler;

            this.IsVisibleChanged += _OnVisibleChanged;
        }

        void _OnVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (IsVisible && Node != null)
                m_DebugControl.SetDebug(Node.Renderer.RunState);
        }
        //protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        //{
        //    base.OnRenderSizeChanged(sizeInfo);

        //    foreach (UIConnector c in m_uiConnectors.Values)
        //    {
        //        c.ResetPos();
        //    }
        //}

        void _DataContextChangedEventHandler(object sender, DependencyPropertyChangedEventArgs e)
        {
            Renderer = DataContext as NodeRendererType;
            Node = Renderer.Owner as NodeType;

            _OnDataContextChanged();

            //SetCanvas(Node.Renderer.RenderCanvas);
            _BuildConnectionBinding();
            if (DebugMgr.Instance.bBreaked)
                m_DebugControl.SetDebug(Node.Renderer.RunState);
            else
                m_DebugControl.SetDebug(NodeState.NS_INVALID);

            Renderer.SelectEvent += Renderer_SelectEvent;
            Renderer.DebugEvent += m_DebugControl.Renderer_DebugEvent;
        }

        private void Renderer_SelectEvent()
        {
            SelectHandler(this, true);
        }

        protected virtual void _OnDataContextChanged()
        {

        }

        private void _BuildConnectionBinding()
        {
            foreach (Connector ctr in Node.Conns.AllConnectors)
            {
                _BuildConnectionBinding(ctr);
            }
        }

        private void _BuildConnectionBinding(Connector ctr)
        {
            if (m_uiConnectors.TryGetValue(ctr, out UIConnector uiConnector))
            {
                uiConnector.DataContext = ctr.Renderer;
            }
        }

        Storyboard m_InstantAnim;
        public Storyboard InstantAnim { get { return m_InstantAnim; } }

        void _OnClick(Point pos)
        {
            m_Operation.MakeCanvasFocused();

            SelectHandler(this, true);
        }

        void _OnDrag(Vector delta, Point pos)
        {
            if (NetworkMgr.Instance.IsConnected)
                return;
            if (Node != null)
            {
                Node.Renderer.DragMain(delta, (System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Shift) != 0 ? 0 : 1);
            }
        }

        void _OnFinishDrag(Vector delta, Point pos)
        {
            if (NetworkMgr.Instance.IsConnected || Node == null)
                return;

            Node.Renderer.FinishDrag(delta, pos);
        }

        public void SetSelect(bool bSelect)
        {
            if (bSelect)
            {
                this.SelectCoverUI.Visibility = Visibility.Visible;
            }
            else
                this.SelectCoverUI.Visibility = Visibility.Collapsed;

            _OnSelect(bSelect);
        }

        protected virtual void _OnSelect(bool bSelect) { }

        public void OnDelete(int param)
        {
            Renderer.Delete(param);
        }

        public void OnDuplicated(int param)
        {
            WorkBenchMgr.Instance.CloneTreeNodeToBench(Node, param != 0);
        }

        public void OnCopied(int param)
        {
            WorkBenchMgr.Instance.CopyNode(Node, param != 0);
        }

        public void ToggleBreakPoint()
        {
            if (Node.DebugPointInfo.HasBreakPoint)
                Node.SetDebugPoint(0);
            else
                Node.SetDebugPoint(1);
        }

        public void ToggleLogPoint()
        {
            if (Node.DebugPointInfo.HasLogPoint)
                Node.SetDebugPoint(0);
            else
                Node.SetDebugPoint(-1);
        }

        public void ToggleDisable()
        {
            Renderer.ToggleDisabled();
        }
    }
}
