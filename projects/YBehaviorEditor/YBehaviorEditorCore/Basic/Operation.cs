using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace YBehavior.Editor.Core.New
{
    public interface IGetCanvas
    {
        FrameworkElement Canvas { get; }
    }

    /// <summary>
    /// A wrapper for the mouse event handlers
    /// </summary>
    public class Operation
    {
        public delegate void ClickHandler(Point absPos);
        public delegate void DragHandler(Vector delta, Point absPos);

        ClickHandler m_LeftClickHandler;
        ClickHandler m_RightClickHandler;
        ClickHandler m_MiddleClickHandler;

        ClickHandler m_LeftDoubleClickHandler;

        DragHandler m_LeftDragHandler;
        DragHandler m_LeftStartDragHandler;
        DragHandler m_LeftFinishDragHandler;

        DragHandler m_RightDragHandler;
        DragHandler m_RightStartDragHandler;
        DragHandler m_RightFinishDragHandler;

        DragHandler m_MiddleDragHandler;
        DragHandler m_MiddleStartDragHandler;
        DragHandler m_MiddleFinishDragHandler;

        IGetCanvas m_Target;
        FrameworkElement RenderCanvas { get { return m_Target != null ? m_Target.Canvas : null; } }

        int m_ValidButtonMask = 0;

        /// <summary>
        /// Register handlers to the UIElement
        /// </summary>
        /// <param name="target"></param>
        public Operation(UIElement target)
        {
            target.MouseDown -= _MouseDown;
            target.MouseDown += _MouseDown;
            target.MouseMove -= _MouseMove;
            target.MouseMove += _MouseMove;
            target.PreviewMouseMove -= _PreviewMouseMove;
            target.PreviewMouseMove += _PreviewMouseMove;
            target.MouseUp -= _MouseUp;
            target.MouseUp += _MouseUp;

            m_Target = target as IGetCanvas;
        }

        /// <summary>
        /// Try to focus on this canvas
        /// </summary>
        public void MakeCanvasFocused()
        {
            //RenderCanvas.Panel.Focus();
            var canvas = RenderCanvas;
            if (canvas != null)
                canvas.Focus();
        }

        public void RegisterLeftClick(ClickHandler handler)
        {
            m_LeftClickHandler = handler;
            m_ValidButtonMask |= (1 << (int)MouseButton.Left);
        }
        public void RegisterRightClick(ClickHandler handler)
        {
            m_RightClickHandler = handler;
            m_ValidButtonMask |= (1 << (int)MouseButton.Right);
        }
        public void RegisterMiddleClick(ClickHandler handler)
        {
            m_MiddleClickHandler = handler;
            m_ValidButtonMask |= (1 << (int)MouseButton.Middle);
        }

        public void RegisterLeftDoubleClick(ClickHandler handler)
        {
            m_LeftDoubleClickHandler = handler;
            m_ValidButtonMask |= (1 << (int)MouseButton.Left);

            _CreateDoubleClickTimer();
        }

        void _CreateDoubleClickTimer()
        {
            if (m_Timer == null)
            {
                m_Timer = new System.Windows.Threading.DispatcherTimer();
                m_Timer.Interval = new TimeSpan(0, 0, 0, 0, 80);
                m_Timer.Tick += (s, e1) => { m_Timer.Stop();/* if (m_ClickHandler != null) m_ClickHandler(m_Pos); */};
            }
        }

        public void RegisterLeftDrag(DragHandler handler, DragHandler starthandler, DragHandler finishhandler)
        {
            m_LeftDragHandler = handler;
            m_LeftStartDragHandler = starthandler;
            m_LeftFinishDragHandler = finishhandler;
            m_ValidButtonMask |= (1 << (int)MouseButton.Left);
        }
        public void RegisterMiddleDrag(DragHandler handler, DragHandler starthandler, DragHandler finishhandler)
        {
            m_MiddleDragHandler = handler;
            m_MiddleStartDragHandler = starthandler;
            m_MiddleFinishDragHandler = finishhandler;
            m_ValidButtonMask |= (1 << (int)MouseButton.Middle);
        }
        public void RegisterRightDrag(DragHandler handler, DragHandler starthandler, DragHandler finishhandler)
        {
            m_RightDragHandler = handler;
            m_RightStartDragHandler = starthandler;
            m_RightFinishDragHandler = finishhandler;
            m_ValidButtonMask |= (1 << (int)MouseButton.Right);
        }

        DragHandler m_DragHandler = null;
        ClickHandler m_ClickHandler = null;
        MouseButton m_PressedButton;
        bool m_bStartClick = false;
        bool m_bStartDrag = false;
        Point m_StartPos = new Point();
        Point m_Pos = new Point();

        System.Windows.Threading.DispatcherTimer m_Timer;
        bool m_bDoubleClick;

        void _MouseDown(object sender, MouseButtonEventArgs e)
        {
            if ((m_ValidButtonMask & (1 << (int)e.ChangedButton)) == 0)
                return;

            FrameworkElement tmp = (FrameworkElement)sender;
            tmp.CaptureMouse();
            m_bStartClick = true;
            m_bStartDrag = true;
            m_Pos = e.GetPosition(RenderCanvas);
            m_StartPos = m_Pos;
            m_PressedButton = e.ChangedButton;
            e.Handled = true;

            m_bDoubleClick = m_Timer != null && m_Timer.IsEnabled;

            switch (m_PressedButton)
            {
                case MouseButton.Left:
                    m_ClickHandler = m_bDoubleClick ? m_LeftDoubleClickHandler : m_LeftClickHandler;
                    m_DragHandler = m_LeftFinishDragHandler;
                    break;
                case MouseButton.Middle:
                    m_ClickHandler = m_MiddleClickHandler;
                    m_DragHandler = m_MiddleFinishDragHandler;
                    break;
                case MouseButton.Right:
                    m_ClickHandler = m_RightClickHandler;
                    m_DragHandler = m_RightFinishDragHandler;
                    break;
                default:
                    break;
            }

            if (m_bDoubleClick)
                m_Timer.Stop();
        }
        void _PreviewMouseMove(object sender, MouseEventArgs e)
        {
            FrameworkElement tmp = (FrameworkElement)sender;
            if (!tmp.IsMouseCaptured)
                return;
            if (!m_bStartDrag)
                return;
            ///> Trigger only once
            if (!m_bStartClick)
                return;
            DragHandler dragHandler = null;
            bool bValid = false;
            if (e.LeftButton == MouseButtonState.Pressed && m_PressedButton == MouseButton.Left)
            {
                dragHandler = m_LeftStartDragHandler;
                bValid = true;
            }
            else if (e.MiddleButton == MouseButtonState.Pressed && m_PressedButton == MouseButton.Middle)
            {
                dragHandler = m_MiddleStartDragHandler;
                bValid = true;
            }
            else if (e.RightButton == MouseButtonState.Pressed && m_PressedButton == MouseButton.Right)
            {
                dragHandler = m_RightStartDragHandler;
                bValid = true;
            }

            if (bValid)
            {
                m_bStartClick = false;
                if (dragHandler != null)
                {
                    dragHandler(new Vector(), m_Pos);
                }
            }
        }

        void _MouseMove(object sender, MouseEventArgs e)
        {
            FrameworkElement tmp = (FrameworkElement)sender;
            if (!tmp.IsMouseCaptured)
                return;
            if (!m_bStartDrag)
                return;

            DragHandler dragHandler = null;
            bool bValid = false;
            if (e.LeftButton == MouseButtonState.Pressed && m_PressedButton == MouseButton.Left)
            {
                dragHandler = m_LeftDragHandler;
                bValid = true;
            }
            else if (e.MiddleButton == MouseButtonState.Pressed && m_PressedButton == MouseButton.Middle)
            {
                dragHandler = m_MiddleDragHandler;
                bValid = true;
            }
            else if (e.RightButton == MouseButtonState.Pressed && m_PressedButton == MouseButton.Right)
            {
                dragHandler = m_RightDragHandler;
                bValid = true;
            }

            if (bValid)
            {
                Point newPos = e.GetPosition(RenderCanvas);
                if (dragHandler != null && newPos != m_Pos)
                {
                    Vector vector = newPos - m_Pos;
                    if (vector.LengthSquared < 9)
                        return;
                    dragHandler(vector, newPos);
                    m_Pos = newPos;
                }
            }
        }
        void _MouseUp(object sender, MouseButtonEventArgs e)
        {
            FrameworkElement tmp = (FrameworkElement)sender;
            tmp.ReleaseMouseCapture();

            bool bValid = false;
            if (e.ChangedButton == m_PressedButton)
            {
                bValid = true;
            }

            if (bValid)
            {
                if (m_bStartClick)
                {
                    m_bStartClick = false;
                    m_bStartDrag = false;

                    ///> It's first click
                    if (!m_bDoubleClick && m_Timer != null)
                    {
                        m_Timer.Start();
                    }
                    //else
                    {
                        if (m_ClickHandler != null)
                            m_ClickHandler(m_Pos);
                    }
                }

                if (m_bStartDrag)
                {
                    m_bStartDrag = false;

                    if (m_DragHandler != null)
                        m_DragHandler(m_Pos - m_StartPos, m_Pos);
                }
                e.Handled = true;
            }
        }

        List<DependencyObject> m_HitTestResult = new List<DependencyObject>();
        public List<DependencyObject> HitTesting(Point pos)
        {
            m_HitTestResult.Clear();
            VisualTreeHelper.HitTest(
                RenderCanvas,
                null,
                new HitTestResultCallback(MyHitTestResult),
                new PointHitTestParameters(pos));

            return m_HitTestResult;
        }

        public HitTestResultBehavior MyHitTestResult(HitTestResult result)
        {
            // Add the hit test result to the list that will be processed after the enumeration.
            m_HitTestResult.Add(result.VisualHit);

            // Set the behavior to return visuals at all z-order levels.
            return HitTestResultBehavior.Continue;
        }

    }
}
