using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using YBehavior.Editor.Core;

namespace YBehavior.Editor.Core
{
    public interface IDragable
    {
        void SetDragged(bool bDragged);
    }
    public interface IDropable
    {
        void SetDropped(bool bDropped);
        void OnDropped(IDragable dragable);
    }

    public delegate void DragHandler(IDragable obj, bool bState);
    public delegate void DropHandler(IDropable obj);

    class DragDropMgr : Singleton<DragDropMgr>
    {
        IDragable m_Dragging;

        public void Clear()
        {
            if (m_Dragging != null)
                m_Dragging.SetDragged(false);
            m_Dragging = null;
        }

        public void OnDragged(IDragable dragging, bool bState)
        {
            if (dragging == null)
                return;

            if (bState)
            {
                if (m_Dragging == dragging)
                    return;

                if (m_Dragging != null)
                    m_Dragging.SetDragged(false);
                m_Dragging = dragging;
                m_Dragging.SetDragged(true);
            }
            else
            {
                if (m_Dragging == dragging)
                    return;
                if (m_Dragging != null)
                    m_Dragging.SetDragged(false);
                m_Dragging = null;
            }
        }

        public void OnDropped(IDropable dropping)
        {
            if (dropping == null)
                return;
            dropping.SetDropped(false);

            if (m_Dragging == null)
                return;

            dropping.OnDropped(m_Dragging);

            Clear();
        }
    }
}
