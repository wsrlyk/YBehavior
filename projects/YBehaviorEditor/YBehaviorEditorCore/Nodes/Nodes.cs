using System;
using System.Collections.Generic;
using System.Text;

namespace YBehavior.Editor.Core
{
    public class Tree : SingleChildNode
    {
        public Tree()
        {
            m_Name = "Root";
            m_Type = NodeType.NT_Root;
            m_Hierachy = NodeHierachy.NH_None;
        }
    }

    class SequenceNode : CompositeNode
    {
        public SequenceNode()
        {
            m_Name = "Sequence";
            m_Type = NodeType.NT_Sequence;
            m_Hierachy = NodeHierachy.NH_Sequence;
        }
    }
    class SelectorNode : CompositeNode
    {
        public SelectorNode()
        {
            m_Name = "Selector";
            m_Type = NodeType.NT_Selector;
            m_Hierachy = NodeHierachy.NH_Selector;
        }
    }

    class CalculatorNode : LeafNode
    {
        public CalculatorNode()
        {
            m_Name = "Calculator";
            m_Type = NodeType.NT_Calculator;
            m_Hierachy = NodeHierachy.NH_Action;
        }
    }

    class NotNode : SingleChildNode
    {
        public NotNode()
        {
            m_Name = "Not";
            m_Type = NodeType.NT_Not;
            m_Hierachy = NodeHierachy.NH_Decorator;
        }
    }

    class AlwaysSuccessNode : SingleChildNode
    {
        public AlwaysSuccessNode()
        {
            m_Name = "AlwaysSuccess";
            m_Type = NodeType.NT_AlwaysSuccess;
            m_Hierachy = NodeHierachy.NH_Decorator;
        }
    }
}
