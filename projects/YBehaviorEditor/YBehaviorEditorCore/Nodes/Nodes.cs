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
                return WorkBenchMgr.Instance.ActiveWorkBench != null ? WorkBenchMgr.Instance.ActiveWorkBench.MainTree: null;
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
            m_Type = NodeType.NT_Default;
            m_Hierachy = NodeHierachy.NH_Compositor;
        }

        public override string Icon => "➜➜➜";
    }
    class SelectorNode : CompositeNode
    {
        public SelectorNode()
        {
            m_Name = "Selector";
            m_Type = NodeType.NT_Default;
            m_Hierachy = NodeHierachy.NH_Compositor;
        }

        public override string Icon => "？？？";
    }

    class RandomSequenceNode : CompositeNode
    {
        public RandomSequenceNode()
        {
            m_Name = "RandomSequence";
            m_Type = NodeType.NT_Default;
            m_Hierachy = NodeHierachy.NH_Compositor;
        }

        public override string Icon => "～➜➜➜";
    }
    class RandomSelectorNode : CompositeNode
    {
        public RandomSelectorNode()
        {
            m_Name = "RandomSelector";
            m_Type = NodeType.NT_Default;
            m_Hierachy = NodeHierachy.NH_Compositor;
        }

        public override string Icon => "～？？？";
    }

    class ActionNode : LeafNode
    {
        public string NoteFormat { get; set; }
        public string ClassName { get; set; }
        public override string Name => ClassName;
        public override string Icon => "▶";

        public ActionNode()
        {
            m_Name = "Action";
            m_Type = NodeType.NT_Invalid;
            m_Hierachy = NodeHierachy.NH_Custom;
        }

        public override Node Clone()
        {
            ActionNode node = base.Clone() as ActionNode;
            node.NoteFormat = this.NoteFormat;
            node.ClassName = this.ClassName;


            return node;
        }
        public override string Note
        {
            get
            {
                string[] values = new string[m_Variables.Datas.Count];
                int i = 0;
                foreach (var v in m_Variables.Datas.Values)
                {
                    values[i++] = v.NoteValue;
                }
                return string.Format(NoteFormat, values);
            }
        }
    }

    class CalculatorNode : LeafNode
    {
        static Dictionary<string, string> s_OperatorDic = new Dictionary<string, string>() { { "ADD", "+" }, { "SUB", "-" }, { "MUL", "*" }, { "DIV", "/" } };

        public override string Icon => "+-×÷";
        public CalculatorNode()
        {
            m_Name = "Calculator";
            m_Type = NodeType.NT_Default;
            m_Hierachy = NodeHierachy.NH_DefaultAction;

        }

        public override void CreateVariables()
        {
            Variable optr = Variables.CreateVariableInNode(
                "Operator",
                "ADD",
                Variable.CreateParams_Enum,
                Variable.CountType.CT_SINGLE,
                Variable.VariableType.VBT_Const,
                true,
                0,
                "ADD|SUB|MUL|DIV"
            );

            Variable opl = Variables.CreateVariableInNode(
                "Opl",
                "0",
                Variable.CreateParams_AllNumbers,
                Variable.CountType.CT_SINGLE,
                Variable.VariableType.VBT_Pointer,
                true,
                1
            );

            Variable opr1 = Variables.CreateVariableInNode(
                "Opr1",
                "0",
                Variable.CreateParams_AllNumbers,
                Variable.CountType.CT_SINGLE,
                Variable.VariableType.VBT_Const,
                false,
                1
            );

            Variable opr2 = Variables.CreateVariableInNode(
                "Opr2",
                "0",
                Variable.CreateParams_AllNumbers,
                Variable.CountType.CT_SINGLE,
                Variable.VariableType.VBT_Const,
                false,
                1
            );
        }

        public override string Note
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("{0} <= {1} {2} {3}",
                    Variables.GetVariable("Opl").NoteValue,
                    Variables.GetVariable("Opr1").NoteValue,
                    s_OperatorDic[Variables.GetVariable("Operator").Value],
                    Variables.GetVariable("Opr2").NoteValue);
                return sb.ToString();
            }
        }
    }

    class ComparerNode : LeafNode
    {
        //static Dictionary<string, string> s_OperatorDic = new Dictionary<string, string>() { { "ADD", "+" }, { "SUB", "-" }, { "MUL", "*" }, { "DIV", "/" } };
        public override string Icon => "x ？y";

        public ComparerNode()
        {
            m_Name = "Comparer";
            m_Type = NodeType.NT_Default;
            m_Hierachy = NodeHierachy.NH_DefaultAction;

        }

        public override void CreateVariables()
        {
            Variable optr = Variables.CreateVariableInNode(
                "Operator",
                "==",
                Variable.CreateParams_Enum,
                Variable.CountType.CT_SINGLE,
                Variable.VariableType.VBT_Const,
                true,
                0,
                "==|!=|>|<|>=|<="
            );

            Variable opl = Variables.CreateVariableInNode(
                "Opl",
                "0",
                Variable.CreateParams_AllNumbers,
                Variable.CountType.CT_SINGLE,
                Variable.VariableType.VBT_Pointer,
                true,
                1
            );

            Variable opr = Variables.CreateVariableInNode(
                "Opr",
                "0",
                Variable.CreateParams_AllNumbers,
                Variable.CountType.CT_SINGLE,
                Variable.VariableType.VBT_Const,
                false,
                1
            );
        }

        public override string Note
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("{0} {1} {2} ?",
                    Variables.GetVariable("Opl").NoteValue,
                    Variables.GetVariable("Operator").Value,
                    Variables.GetVariable("Opr").NoteValue
                    );
                return sb.ToString();
            }
        }
    }

    class SetDataNode : LeafNode
    {
        public override string Icon => "x<=y";

        public SetDataNode()
        {
            m_Name = "SetData";
            m_Type = NodeType.NT_Default;
            m_Hierachy = NodeHierachy.NH_DefaultAction;

        }

        public override void CreateVariables()
        {
            Variable opl = Variables.CreateVariableInNode(
                "Target",
                "0",
                Variable.CreateParams_AllTypes,
                Variable.CountType.CT_SINGLE,
                Variable.VariableType.VBT_Pointer,
                true,
                1
            );

            Variable opr = Variables.CreateVariableInNode(
                "Source",
                "0",
                Variable.CreateParams_AllTypes,
                Variable.CountType.CT_SINGLE,
                Variable.VariableType.VBT_Const,
                false,
                1
            );
        }

        public override string Note
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("{0} <= {1}",
                    Variables.GetVariable("Target").NoteValue,
                    Variables.GetVariable("Source").NoteValue
                    );
                return sb.ToString();
            }
        }
    }

    class AlwaysSuccessNode : SingleChildNode
    {
        public override string Icon => "✔";

        public AlwaysSuccessNode()
        {
            m_Name = "AlwaysSuccess";
            m_Type = NodeType.NT_Default;
            m_Hierachy = NodeHierachy.NH_Decorator;
        }
    }

    class AlwaysFailureNode : SingleChildNode
    {
        public override string Icon => "✘";

        public AlwaysFailureNode()
        {
            m_Name = "AlwaysFailure";
            m_Type = NodeType.NT_Default;
            m_Hierachy = NodeHierachy.NH_Decorator;
        }
    }

    class InvertorNode : SingleChildNode
    {
        public override string Icon => "!";

        public InvertorNode()
        {
            m_Name = "Invertor";
            m_Type = NodeType.NT_Default;
            m_Hierachy = NodeHierachy.NH_Decorator;
        }
    }

    class IfThenElseNode : Node
    {
        public override string Icon => "↙ ？↘";

        public IfThenElseNode()
        {
            m_Name = "IfThenElse";
            m_Type = NodeType.NT_Default;
            m_Hierachy = NodeHierachy.NH_Compositor;

            new ConnectionSingle(this, "if");
            new ConnectionSingle(this, "then");
            new ConnectionSingle(this, "else");
        }
    }

    class RandomNode : LeafNode
    {
        public override string Icon => "～";

        public RandomNode()
        {
            m_Name = "Random";
            m_Type = NodeType.NT_Default;
            m_Hierachy = NodeHierachy.NH_DefaultAction;

        }

        public override void CreateVariables()
        {
            Variable opl = Variables.CreateVariableInNode(
                "Target",
                "0",
                Variable.CreateParams_RandomTypes,
                Variable.CountType.CT_SINGLE,
                Variable.VariableType.VBT_Pointer,
                true,
                1
            );

            Variable opr1 = Variables.CreateVariableInNode(
                "Bound1",
                "0",
                Variable.CreateParams_RandomTypes,
                Variable.CountType.CT_SINGLE,
                Variable.VariableType.VBT_Const,
                false,
                1
            );

            Variable opr2 = Variables.CreateVariableInNode(
                "Bound2",
                "0",
                Variable.CreateParams_RandomTypes,
                Variable.CountType.CT_SINGLE,
                Variable.VariableType.VBT_Const,
                false,
                1
            );
        }

        public override string Note
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("{0} <= {{ {1} ~ {2} }}",
                    Variables.GetVariable("Target").NoteValue,
                    Variables.GetVariable("Bound1").NoteValue,
                    Variables.GetVariable("Bound2").NoteValue);
                return sb.ToString();
            }
        }
    }
}
