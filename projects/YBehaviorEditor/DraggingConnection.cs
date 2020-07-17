using System;
using System.Windows;
using System.Windows.Controls;
using YBehavior.Editor.Core.New;

namespace YBehavior.Editor
{
    public interface IDraggingConnection
    {
        void Set(Point start, Point end, double middle);
    }

    public class DraggingConnection<T> : Singleton<DraggingConnection<T>> where T: YUserControl, IDraggingConnection
    {
        Panel m_Canvas;
        public Panel Canvas { get { return m_Canvas; } }

        T m_Connection;

        bool m_bDragging = false;

        public void SetCanvas(Panel canvas)
        {
            m_Canvas = canvas;
        }

        public void Drag(Point from, Point to)
        {
            if (m_Canvas == null)
                return;

            if (m_Connection == null)
                m_Connection = Activator.CreateInstance(typeof(T), new object[]{false}) as T;

            if (!m_bDragging)
            {
                m_bDragging = true;
                m_Canvas.Children.Add(m_Connection);
            }

            m_Connection.Set(from, to, (from.Y + to.Y) / 2);

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
