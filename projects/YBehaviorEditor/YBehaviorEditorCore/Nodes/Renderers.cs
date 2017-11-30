using System;
using System.Collections.Generic;
using System.Text;

namespace YBehavior.Editor.Core
{
    class SequenceRenderer : CompositeRenderer
    {
        public SequenceRenderer()
        {
            m_Name = "Sequence";
            m_Type = NodeType.NT_Sequence;
        }
    }
    class SelectorRenderer : CompositeRenderer
    {
        public SelectorRenderer()
        {
            m_Name = "Selector";
            m_Type = NodeType.NT_Selector;
        }
    }

    class CalculatorRenderer : LeafRenderer
    {
        public CalculatorRenderer()
        {
            m_Name = "Calculator";
            m_Type = NodeType.NT_Calculator;
        }
    }

    class RootRenderer : SingleChildRenderer
    {
        public RootRenderer()
        {
            m_Name = "Root";
            m_Type = NodeType.NT_Root;
        }
    }

    class NotRenderer : SingleChildRenderer
    {
        public NotRenderer()
        {
            m_Name = "Not";
            m_Type = NodeType.NT_Not;
        }
    }

    class AlwaysSuccessRenderer : SingleChildRenderer
    {
        public AlwaysSuccessRenderer()
        {
            m_Name = "AlwaysSuccess";
            m_Type = NodeType.NT_AlwaysSuccess;
        }
    }
}
