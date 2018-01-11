using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace YBehavior.Editor.Core
{
    public class DraggingConnection : Singleton<DraggingConnection>
    {
        Canvas m_Canvas;
        public Canvas Canvas { get { return m_Canvas; } }

        UIConnection m_Connection;

        bool m_bDragging = false;

        public void SetCanvas(Canvas canvas)
        {
            m_Canvas = canvas;
        }

        public void Drag(Point from, Point to)
        {
            if (m_Canvas == null)
                return;

            if (m_Connection == null)
                m_Connection = new UIConnection();

            if (!m_bDragging)
            {
                m_bDragging = true;
                m_Canvas.Children.Add(m_Connection);
            }

            m_Connection.SetWithMidY(from, to, (from.Y + to.Y) / 2);

        }

        public void FinishDrag()
        {
            if (m_Connection == null || m_Canvas == null)
                return;

            m_Canvas.Children.Remove(m_Connection);

            m_bDragging = false;
        }
    }
}
