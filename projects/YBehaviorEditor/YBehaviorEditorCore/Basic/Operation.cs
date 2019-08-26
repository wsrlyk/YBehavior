using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace YBehavior.Editor.Core.New
{
    public interface IHasAncestor
    {
        FrameworkElement Ancestor { get; }
    }

    public class Operation
    {
        public delegate void ClickHandler();
        public delegate void DragHandler(Vector delta, Point absPos);

        ClickHandler m_LeftClickHandler;
        ClickHandler m_RightClickHandler;
        ClickHandler m_MiddleClickHandler;

        DragHandler m_LeftDragHandler;
        DragHandler m_LeftStartDragHandler;
        DragHandler m_LeftFinishDragHandler;

        DragHandler m_MiddleDragHandler;
        DragHandler m_MiddleStartDragHandler;
        DragHandler m_MiddleFinishDragHandler;

        IHasAncestor m_Target;
        FrameworkElement RenderCanvas { get { return m_Target != null ? m_Target.Ancestor : null; } }

        int m_ValidButtonMask = 0;

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

            m_Target = target as IHasAncestor;
        }

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

        MouseButton m_PressedButton;
        bool m_bStartClick = false;
        bool m_bStartDrag = false;
        Point m_StartPos = new Point();
        Point m_Pos = new Point();

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
        }
        void _PreviewMouseMove(object sender, MouseEventArgs e)
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

            DragHandler dragHandler = null;
            ClickHandler clickHandler = null;
            bool bValid = false;
            if (e.ChangedButton == m_PressedButton)
            {
                bValid = true;
                switch (m_PressedButton)
                {
                    case MouseButton.Left:
                        clickHandler = m_LeftClickHandler;
                        dragHandler = m_LeftFinishDragHandler;
                        break;
                    case MouseButton.Middle:
                        clickHandler = m_MiddleClickHandler;
                        dragHandler = m_MiddleFinishDragHandler;
                        break;
                    case MouseButton.Right:
                        clickHandler = m_RightClickHandler;
                        break;
                    default:
                        break;
                }
            }

            if (bValid)
            {
                if (m_bStartClick)
                {
                    m_bStartClick = false;
                    m_bStartDrag = false;
                    if (clickHandler != null)
                        clickHandler();
                }

                if (m_bStartDrag)
                {
                    m_bStartDrag = false;

                    if (dragHandler != null)
                        dragHandler(m_Pos - m_StartPos, m_Pos);
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
