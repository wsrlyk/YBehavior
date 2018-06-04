﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace YBehavior.Editor.Core
{
    public interface IHasAncestor
    {
        FrameworkElement Ancestor { get; }
    }

    public class Operation
    {
        public delegate void ClickHandler();
        public delegate void DragHandler(Vector delta, Point absPos);

        ClickHandler m_ClickHandler;
        DragHandler m_DragHandler;
        DragHandler m_StartDragHandler;

        IHasAncestor m_Target;
        FrameworkElement RenderCanvas { get { return m_Target != null ? m_Target.Ancestor : null; } }

        public Operation(UIElement target)
        {
            target.MouseLeftButtonDown -= _MouseLeftButtonDown;
            target.MouseLeftButtonDown += _MouseLeftButtonDown;
            target.MouseMove -= _MouseMove;
            target.MouseMove += _MouseMove;
            target.PreviewMouseMove -= _PreviewMouseMove;
            target.PreviewMouseMove += _PreviewMouseMove;
            target.MouseLeftButtonUp -= _MouseLeftButtonUp;
            target.MouseLeftButtonUp += _MouseLeftButtonUp;

            m_Target = target as IHasAncestor;
        }

        public void MakeCanvasFocused()
        {
            //RenderCanvas.Panel.Focus();
            var canvas = RenderCanvas;
            if (canvas != null)
                canvas.Focus();
        }

        public void RegisterClick(ClickHandler handler)
        {
            m_ClickHandler = handler;
        }

        public void RegisterDragDrop(DragHandler handler, DragHandler starthandler)
        {
            m_DragHandler = handler;
            m_StartDragHandler = starthandler;
        }

        public void RegisterDrag(DragHandler handler)
        {
            m_DragHandler = handler;
        }

        bool m_bStartClick = false;
        bool m_bStartDrag = false;
        Point m_Pos = new Point();

        void _MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            FrameworkElement tmp = (FrameworkElement)sender;
            tmp.CaptureMouse();
            m_bStartClick = true;
            m_bStartDrag = true;
            m_Pos = e.GetPosition(RenderCanvas);
            e.Handled = true;
        }
        void _PreviewMouseMove(object sender, MouseEventArgs e)
        {
            FrameworkElement tmp = (FrameworkElement)sender;
            if (!tmp.IsMouseCaptured)
                return;
            if (!m_bStartDrag)
                return;
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                m_bStartClick = false;
                if (m_StartDragHandler != null)
                    m_StartDragHandler(new Vector(), m_Pos);
            }
        }

        void _MouseMove(object sender, MouseEventArgs e)
        {
            FrameworkElement tmp = (FrameworkElement)sender;
            if (!tmp.IsMouseCaptured)
                return;
            if (!m_bStartDrag)
                return;
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Point newPos = e.GetPosition(RenderCanvas);
                if (m_DragHandler != null && newPos != m_Pos)
                {
                    Vector vector = newPos - m_Pos;
                    if (vector.LengthSquared < 9)
                        return;
                    m_DragHandler(vector, newPos);
                    m_Pos = newPos;
                }
            }
        }
        void _MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            FrameworkElement tmp = (FrameworkElement)sender;
            tmp.ReleaseMouseCapture();

            if (m_bStartClick)
            {
                m_bStartClick = false;

                if (m_ClickHandler != null)
                    m_ClickHandler();
            }

            if (m_bStartDrag)
            {
                m_bStartDrag = false;

                if (m_DragHandler != null)
                    m_DragHandler(new Vector(0, 0), m_Pos);
            }
            e.Handled = true;
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
