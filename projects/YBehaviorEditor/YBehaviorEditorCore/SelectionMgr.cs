﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using YBehavior.Editor.Core;

namespace YBehavior.Editor.Core
{
    public interface ISelectable
    {
        void SetSelect(bool bSelect);
    }

    public delegate void SelectionStateChangeHandler(ISelectable obj, bool bState);

    class SelectionMgr : Singleton<SelectionMgr>
    {
        List<ISelectable> m_Selections = new List<ISelectable>();
        ISelectable m_SingleSelection;

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

    }
}
