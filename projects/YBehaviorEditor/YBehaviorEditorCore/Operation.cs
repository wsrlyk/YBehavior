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
    class Operation
    {
        public delegate void ClickHandler();
        public delegate void DragHandler(Vector delta, Point absPos);

        ClickHandler m_ClickHandler;
        DragHandler m_DragHandler;
        DragHandler m_StartDragHandler;
        Panel m_Panel;
        public Operation(UIElement target, Panel panel)
        {
            m_Panel = panel;
            target.MouseLeftButtonDown += _MouseLeftButtonDown;
            target.MouseMove += _MouseMove;
            target.PreviewMouseMove += _PreviewMouseMove;
            target.MouseLeftButtonUp += _MouseLeftButtonUp;
        }

        public void SetPanel(Panel panel)
        {
            m_Panel = panel;
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
            m_Pos = e.GetPosition(m_Panel);
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
                Point newPos = e.GetPosition(m_Panel);
                if (m_DragHandler != null)
                    m_DragHandler(newPos - m_Pos, newPos);
                m_Pos = newPos;
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
        }

        List<DependencyObject> m_HitTestResult = new List<DependencyObject>();
        public List<DependencyObject> HitTesting(Point pos)
        {
            m_HitTestResult.Clear();
            VisualTreeHelper.HitTest(
                m_Panel, 
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
