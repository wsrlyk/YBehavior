using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace YBehavior.Editor.Core
{
    class Operation
    {
        public delegate void ClickHandler();
        public delegate void DragHandler(Vector delta, Point absPos);

        ClickHandler m_ClickHandler;
        DragHandler m_DragHandler;

        public Operation(System.Windows.Controls.UserControl userControl)
        {
            userControl.MouseLeftButtonDown += _MouseLeftButtonDown;
            userControl.MouseMove += _MouseMove;
            userControl.MouseLeftButtonUp += _MouseLeftButtonUp;
        }

        public void RegisterClick(ClickHandler handler)
        {
            m_ClickHandler = handler;
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
            m_Pos = e.GetPosition(null);
        }
        void _MouseMove(object sender, MouseEventArgs e)
        {
            FrameworkElement tmp = (FrameworkElement)sender;
            if (!tmp.IsMouseCaptured || !m_bStartDrag)
                return;
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                m_bStartClick = false;

                Point newPos = e.GetPosition(null);
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

            m_bStartDrag = false;
        }

    }
}
