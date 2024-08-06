namespace YBehavior.Editor.Core.New
{
    /// <summary>
    /// Interface of objects can be dragged
    /// </summary>
    public interface IDragable
    {
        /// <summary>
        /// Called when dragging state changed
        /// </summary>
        /// <param name="bDragged"></param>
        void SetDragged(bool bDragged);
    }
    /// <summary>
    /// Interface of objects can be dropped
    /// </summary>
    public interface IDropable
    {
        /// <summary>
        /// Called when dropping state changed
        /// </summary>
        void SetDropped(bool bDropped);
        /// <summary>
        /// Called when a dragable is dropped
        /// </summary>
        /// <param name="dragable"></param>
        void OnDropped(IDragable dragable);
    }

    public delegate void DragHandler(IDragable obj, bool bState);
    public delegate void DropHandler(IDropable obj);

    /// <summary>
    /// Drag and drop management
    /// </summary>
    public class DragDropMgr : Singleton<DragDropMgr>
    {
        /// <summary>
        /// Current dragging object
        /// </summary>
        IDragable m_Dragging;
        /// <summary>
        /// Current dropping object
        /// </summary>
        IDropable m_Dropping;

        /// <summary>
        /// Clear the current drag and drop objects
        /// </summary>
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

        /// <summary>
        /// Called when an object is dragged or not
        /// </summary>
        /// <param name="dragging">object</param>
        /// <param name="bState">dragged or not</param>
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

        /// <summary>
        /// Called when an object is on hover
        /// </summary>
        /// <param name="dropping"></param>
        public void OnHover(IDropable dropping)
        {
            if (m_Dropping != null && m_Dropping != dropping)
                m_Dropping.SetDropped(false);
            m_Dropping = dropping;
            if (m_Dropping != null)
                m_Dropping.SetDropped(true);
        }

        /// <summary>
        /// Called when an object is dropped by the current dragging object
        /// </summary>
        /// <param name="dropping"></param>
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
