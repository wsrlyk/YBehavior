using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace YBehavior.Editor.Core.New
{
    public interface ISelectable
    {
        void SetSelect(bool bSelect);
    }

    public interface IDeletable
    {
        void OnDelete(int param);
    }

    public interface IDuplicatable
    {
        void OnDuplicated(int param);
        void OnCopied(int param);
    }
    public interface IDebugPointable
    {
        void ToggleBreakPoint();
        void ToggleLogPoint();
    }

    public interface ICanDisable
    {
        void ToggleDisable();
    }

    public interface IHasCondition
    {
        void ToggleCondition();
    }

    public interface ICanFold
    {
        void ToggleFold();
    }

    public interface ICanMakeDefault
    {
        void MakeDefault();
    }

    public delegate void SelectionStateChangeHandler(ISelectable obj, bool bState);
    public delegate void DeleteHandler(IDeletable obj);

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

        public void OnSingleSelectedChange(ISelectable selection, bool bState)
        {
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

        public void OnMultiSelectedChange(ISelectable selection, bool bState)
        {
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

        public void TryDeleteSelection(int param)
        {
            if (m_SingleSelection == null)
                return;

            IDeletable deletable = m_SingleSelection as IDeletable;
            if (deletable != null)
            {
                deletable.OnDelete(param);
                m_SingleSelection = null;

                _FireSelectionEvent();
            }
        }

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
