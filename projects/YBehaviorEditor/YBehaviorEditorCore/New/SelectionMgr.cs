using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace YBehavior.Editor.Core.New
{
    /// <summary>
    /// Interface of selectable
    /// </summary>
    public interface ISelectable
    {
        /// <summary>
        /// Toggle state of selection
        /// </summary>
        /// <param name="bSelect"></param>
        void SetSelect(bool bSelect);
    }
    /// <summary>
    /// Interface of deletable
    /// </summary>
    public interface IDeletable
    {
        /// <summary>
        /// Called when deleted
        /// </summary>
        /// <param name="param"></param>
        void OnDelete(int param);
    }
    /// <summary>
    /// Interface of duplicatable
    /// </summary>
    public interface IDuplicatable
    {
        /// <summary>
        /// Called when duplicated (copied and pasted)
        /// </summary>
        /// <param name="param"></param>
        void OnDuplicated(int param);
        /// <summary>
        /// Called when copied
        /// </summary>
        /// <param name="param"></param>
        void OnCopied(int param);
    }
    /// <summary>
    /// Interface of that can have debug point
    /// </summary>
    public interface IDebugPointable
    {
        /// <summary>
        /// Toggle break point
        /// </summary>
        void ToggleBreakPoint();
        /// <summary>
        /// Toggle log point
        /// </summary>
        void ToggleLogPoint();
    }
    /// <summary>
    /// Interface of disable
    /// </summary>
    public interface ICanDisable
    {
        /// <summary>
        /// Toggle disable
        /// </summary>
        void ToggleDisable();
    }
    /// <summary>
    /// Interface of that can have condition pin
    /// </summary>
    public interface IHasCondition
    {
        /// <summary>
        /// Toggle condition pin
        /// </summary>
        void ToggleCondition();
    }
    /// <summary>
    /// Interface of that can be folded
    /// </summary>
    public interface ICanFold
    {
        /// <summary>
        /// Toggle fold/unfold
        /// </summary>
        void ToggleFold();
    }
    /// <summary>
    /// Interface of that can be default state
    /// </summary>
    public interface ICanMakeDefault
    {
        /// <summary>
        /// Called when make me the default
        /// </summary>
        void MakeDefault();
    }

    public delegate void SelectionStateChangeHandler(object obj, bool bState);
    public delegate void DeleteHandler(IDeletable obj);
    /// <summary>
    /// Selected object management
    /// </summary>
    public class SelectionMgr : Singleton<SelectionMgr>
    {
        List<ISelectable> m_Selections = new List<ISelectable>();
        ISelectable m_SingleSelection;

        public SelectionMgr()
        {
            EventMgr.Instance.Register(EventType.WorkBenchSelected, _OnWorkBenchSelected);
        }

        private void _OnWorkBenchSelected(EventArg arg)
        {
            Clear();
        }

        private void _FireSelectionEvent()
        {
            SelectionChangedArg arg = new SelectionChangedArg();
            arg.Target = m_SingleSelection;
            EventMgr.Instance.Send(arg);
        }
        /// <summary>
        /// Clear the selections
        /// </summary>
        public void Clear()
        {
            foreach(ISelectable selection in m_Selections)
            {
                selection.SetSelect(false);
            }

            m_Selections.Clear();

            if (m_SingleSelection != null)
                m_SingleSelection.SetSelect(false);
            m_SingleSelection = null;
            _FireSelectionEvent();
        }
        /// <summary>
        /// Select/Unselect an object
        /// </summary>
        /// <param name="o"></param>
        /// <param name="bState"></param>
        public void OnSingleSelectedChange(object o, bool bState)
        {
            ISelectable selection = o as ISelectable;
            if (selection == null)
                return;

            if (bState)
            {
                if (selection == m_SingleSelection)
                    return;

                if (m_SingleSelection != null)
                    m_SingleSelection.SetSelect(false);
                m_SingleSelection = selection;
                m_SingleSelection.SetSelect(true);
            }
            else
            {
                if (selection != m_SingleSelection)
                    return;
                if (m_SingleSelection != null)
                    m_SingleSelection.SetSelect(false);
                m_SingleSelection = null;
            }

            _FireSelectionEvent();
        }
        /// <summary>
        /// Select/Unselect an object with others
        /// </summary>
        /// <param name="o"></param>
        /// <param name="bState"></param>
        public void OnMultiSelectedChange(object o, bool bState)
        {
            ISelectable selection = o as ISelectable;
            if (selection == null)
                return;
            if (bState)
            {
                m_Selections.Add(selection);
                selection.SetSelect(true);
            }
            else
            {
                foreach (ISelectable select in m_Selections)
                {
                    if (select == selection)
                    {
                        selection.SetSelect(false);
                        break;
                    }
                }
            }
        }
        /// <summary>
        /// Delete the single selection
        /// </summary>
        /// <param name="param"></param>
        public void TryDeleteSelection(int param)
        {
            if (m_SingleSelection == null)
                return;

            IDeletable deletable = m_SingleSelection as IDeletable;
            if (deletable != null)
            {
                Clear();

                deletable.OnDelete(param);
            }
        }
        /// <summary>
        /// Duplicate the single selection
        /// </summary>
        /// <param name="param"></param>
        public void TryDuplicateSelection(int param)
        {
            if (m_SingleSelection == null)
                return;

            IDuplicatable duplicatable = m_SingleSelection as IDuplicatable;
            if (duplicatable != null)
            {
                duplicatable.OnDuplicated(param);
            }
        }
        /// <summary>
        /// Copy the single selection
        /// </summary>
        /// <param name="param"></param>
        public void TryCopySelection(int param)
        {
            if (m_SingleSelection == null)
                return;

            IDuplicatable duplicatable = m_SingleSelection as IDuplicatable;
            if (duplicatable != null)
            {
                duplicatable.OnCopied(param);
            }
        }
        /// <summary>
        /// Toggle break point of the single selection
        /// </summary>
        /// <param name="param"></param>
        public void TryToggleBreakPoint()
        {
            if (m_SingleSelection == null)
                return;

            IDebugPointable debugPointable = m_SingleSelection as IDebugPointable;
            if (debugPointable != null)
            {
                debugPointable.ToggleBreakPoint();
            }
        }
        /// <summary>
        /// Toggle log point of the single selection
        /// </summary>
        /// <param name="param"></param>
        public void TryToggleLogPoint()
        {
            if (m_SingleSelection == null)
                return;

            IDebugPointable debugPointable = m_SingleSelection as IDebugPointable;
            if (debugPointable != null)
            {
                debugPointable.ToggleLogPoint();
            }
        }
        /// <summary>
        /// Toggle disable state of the single selection
        /// </summary>
        /// <param name="param"></param>
        public void TryToggleDisable()
        {
            if (m_SingleSelection == null)
                return;

            ICanDisable disable = m_SingleSelection as ICanDisable;
            if (disable != null)
            {
                disable.ToggleDisable();
            }
        }
        /// <summary>
        /// Toggle condition pin of the single selection
        /// </summary>
        /// <param name="param"></param>
        public void TryToggleCondition()
        {
            if (m_SingleSelection == null)
                return;

            IHasCondition condition = m_SingleSelection as IHasCondition;
            if (condition != null)
            {
                condition.ToggleCondition();
            }
        }
        /// <summary>
        /// Toggle fold/unfold of the single selection
        /// </summary>
        /// <param name="param"></param>
        public void TryToggleFold()
        {
            if (m_SingleSelection == null)
                return;

            ICanFold fold = m_SingleSelection as ICanFold;
            if (fold != null)
            {
                fold.ToggleFold();
            }
        }
        /// <summary>
        /// Make the single selection default state
        /// </summary>
        /// <param name="param"></param>
        public void TryMakeDefault()
        {
            if (m_SingleSelection == null)
                return;

            ICanMakeDefault makedefault = m_SingleSelection as ICanMakeDefault;
            if (makedefault != null)
            {
                makedefault.MakeDefault();
            }
        }
    }
}
