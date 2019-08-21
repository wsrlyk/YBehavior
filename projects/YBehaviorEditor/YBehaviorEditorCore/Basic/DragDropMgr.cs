using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace YBehavior.Editor.Core.New
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

    public class DragDropMgr : Singleton<DragDropMgr>
    {
        IDragable m_Dragging;
        IDropable m_Dropping;

        public void Clear()
        {
            if (m_Dragging != null)
            {
                m_Dragging.SetDragged(false);
                m_Dragging = null;
            }
            if (m_Dropping != null)
            {
                m_Dropping.SetDropped(false);
                m_Dropping = null;
            }
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

        public void OnHover(IDropable dropping)
        {
            if (m_Dropping != null && m_Dropping != dropping)
                m_Dropping.SetDropped(false);
            m_Dropping = dropping;
            if (m_Dropping != null)
                m_Dropping.SetDropped(true);
        }

        public void OnDropped(IDropable dropping)
        {
            do
            {
                if (m_Dragging == null)
                    break;
                if (dropping == null)
                    break;

                dropping.SetDropped(false);

                dropping.OnDropped(m_Dragging);
            } while (false);
            Clear();
        }
    }
}
