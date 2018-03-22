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

        protected override bool LoadOtherAttr(XmlAttribute attr)
        {
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

    class ActionNode : LeafNode
    {
        public string NoteFormat { get; set; }
        public string ClassName { get; set; }
        public override string Name => ClassName;

        public ActionNode()
        {
            m_Name = "Action";
            m_Type = NodeType.NT_Invalid;
            m_Hierachy = NodeHierachy.NH_Custom;
        }

        public ActionNode Clone()
        {
            ActionNode other = new ActionNode
            {
                m_Name = this.m_Name,
                m_Type = this.m_Type,
                m_Hierachy = this.m_Hierachy,
                NoteFormat = this.NoteFormat,
                ClassName = this.ClassName
            };

            foreach (var v in m_Variables.Datas.Values)
            {
                Variable newv = v.Clone();
                other.Variables.AddVariable(newv);
            }
            return other;
        }
        public override string Note
        {
            get
            {
                string[] values = new string[m_Variables.Datas.Count];
                int i = 0;
                foreach (var v in m_Variables.Datas.Values)
                {
                    values[i++] = v.Value;
                }
                return string.Format(NoteFormat, values);
            }
        }
    }

    class CalculatorNode : LeafNode
    {
        static Dictionary<string, string> s_OperatorDic = new Dictionary<string, string>() { { "ADD", "+" }, { "SUB", "-" }, { "MUL", "*" }, { "DIV", "/" } };

        public CalculatorNode()
        {
            m_Name = "Calculator";
            m_Type = NodeType.NT_Calculator;
            m_Hierachy = NodeHierachy.NH_DefaultAction;

            Variable optr = Variable.CreateVariableInNode(
                "Operator",
                "ADD",
                Variable.CreateParams_Enum,
                Variable.CountType.CT_SINGLE,
                Variable.VariableType.VBT_Const,
                "ADD|SUB|MUL|DIV"
            );
            optr.AlwaysConst = true;
            Variables.AddVariable(optr);

            Variable opl = Variable.CreateVariableInNode(
                "Opl",
                "0",
                Variable.CreateParams_AllNumbers,
                Variable.CountType.CT_SINGLE,
                Variable.VariableType.VBT_Const
            );
            Variables.AddVariable(opl);

            Variable opr1 = Variable.CreateVariableInNode(
                "Opr1",
                "0",
                Variable.CreateParams_AllNumbers,
                Variable.CountType.CT_SINGLE,
                Variable.VariableType.VBT_Const
            );
            Variables.AddVariable(opr1);

            Variable opr2 = Variable.CreateVariableInNode(
                "Opr2",
                "0",
                Variable.CreateParams_AllNumbers,
                Variable.CountType.CT_SINGLE,
                Variable.VariableType.VBT_Const
            );
            Variables.AddVariable(opr2);
        }

        public override string Note
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("{0} <= {1} {2} {3}",
                    Variables.GetVariable("Opl").Value,
                    Variables.GetVariable("Opr1").Value,
                    s_OperatorDic[Variables.GetVariable("Operator").Value],
                    Variables.GetVariable("Opr2").Value);
                return sb.ToString();
            }
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
