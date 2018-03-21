using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

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

        public static Tree GlobalTree
        {
            get
            {
                return WorkBenchMgr.Instance.ActiveWorkBench.MainTree;
            }
        }
        protected override bool _HasParentHolder()
        {
            return false;
        }

        protected override bool ProcessAttrWhenLoad(XmlAttribute attr)
        {
            if (base.ProcessAttrWhenLoad(attr))
                return true;

            return m_Variables.TryAddData(attr.Name, attr.Value);
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

            Variable optr = Variable.CreateVariableInNode(
                "Operator",
                "ADD",
                Variable.CreateParams_Enum,
                Variable.CreateParams_Single,
                Variable.VariableType.VBT_Const,
                "ADD|SUB|MUL|DIV"
            );
            optr.AlwaysConst = true;
            Variables.AddVariable(optr);

            Variable opl = Variable.CreateVariableInNode(
                "Opl",
                "0",
                Variable.CreateParams_AllNumbers,
                Variable.CreateParams_Single,
                Variable.VariableType.VBT_Const
            );
            Variables.AddVariable(opl);

            Variable opr1 = Variable.CreateVariableInNode(
                "Opr1",
                "0",
                Variable.CreateParams_AllNumbers,
                Variable.CreateParams_Single,
                Variable.VariableType.VBT_Const
            );
            Variables.AddVariable(opr1);

            Variable opr2 = Variable.CreateVariableInNode(
                "Opr2",
                "0",
                Variable.CreateParams_AllNumbers,
                Variable.CreateParams_Single,
                Variable.VariableType.VBT_Const
            );
            Variables.AddVariable(opr2);
        }

        protected override bool ProcessAttrWhenLoad(XmlAttribute attr)
        {
            if (base.ProcessAttrWhenLoad(attr))
                return true;

            Variable v = null;

            switch(attr.Name)
            {
                //case "Operator":
                //    {
                //        v = Variables.GetVariable("Operator");
                //        if (!v.SetVariable(Variable.ENUM, Variable.NONE, Variable.CONST, attr.Value,
                //            "ADD|SUB|MUL|DIV"))
                //            return false;
                //    }
                //    break;
                default:
                    {
                        v = Variables.GetVariable(attr.Name);
                        if (v != null)
                        {
                            if (!v.SetVariableInNode(attr.Value))
                                return false;
                        }
                    }
                    break;
            }

            if (v != null && v.CheckValid())
            {
                return true;
            }
            return false;
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
