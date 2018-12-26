using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace YBehavior.Editor.Core
{
    public class Tree : SingleChildNode
    {
        InOutMemory m_InOutMemory;
        public InOutMemory InOutMemory { get { return m_InOutMemory; } }
        public override InOutMemory InOutData => m_InOutMemory;

        public Tree()
        {
            m_Name = "Root";
            m_Type = NodeType.NT_Root;
            m_Hierachy = NodeHierachy.NH_None;
        }

        protected override void _OnCreateBase()
        {
            m_InOutMemory = new InOutMemory(this, true);
        }

        public static Tree GlobalTree
        {
            get
            {
                return WorkBenchMgr.Instance.ActiveWorkBench != null ? WorkBenchMgr.Instance.ActiveWorkBench.MainTree : null;
            }
        }
        protected override bool _HasParentHolder()
        {
            return false;
        }

        protected override void _OnLoadChild(XmlNode data)
        {
            if (data.Name == "Shared" || data.Name == "Local")
            {
                foreach (System.Xml.XmlAttribute attr in data.Attributes)
                {
                    if (ReservedAttributes.Contains(attr.Name))
                        continue;
                    TreeMemory.TryAddData(attr.Name, attr.Value);
                }
            }
            else if (data.Name == "Input" || data.Name == "Output")
            {
                foreach (System.Xml.XmlAttribute attr in data.Attributes)
                {
                    if (ReservedAttributes.Contains(attr.Name))
                        continue;

                    if(!m_InOutMemory.TryAddData(attr.Name, attr.Value, data.Name == "Input"))
                    {
                        LogMgr.Instance.Error("Error when add Input/Output: " + attr.Name + " " + attr.Value);
                        continue;
                    }
                }
            }
        }

        protected override bool LoadOtherAttr(XmlAttribute attr)
        {
            ///> These shareddatas are moved to sub child nodes
            //return TreeMemory.TryAddData(attr.Name, attr.Value);
            return true;
        }

        protected override void _OnSaveVariables(XmlElement data, XmlDocument xmlDoc)
        {
            _WriteMemory(data, xmlDoc);
        }
        protected override void _OnExportVariables(XmlElement data, XmlDocument xmlDoc)
        {
            _WriteMemory(data, xmlDoc);
        }

        void _WriteMemory(XmlElement data, XmlDocument xmlDoc)
        {
            if (TreeMemory.SharedMemory.Datas.Count > 0)
            {
                XmlElement nodeEl = xmlDoc.CreateElement("Shared");
                data.AppendChild(nodeEl);

                _WriteVariables(TreeMemory.SharedMemory, nodeEl);
            }
            if (TreeMemory.LocalMemory.Datas.Count > 0)
            {
                XmlElement nodeEl = xmlDoc.CreateElement("Local");
                data.AppendChild(nodeEl);

                _WriteVariables(TreeMemory.LocalMemory, nodeEl);
            }
            if (InOutMemory.InputMemory.Datas.Count > 0)
            {
                XmlElement nodeEl = xmlDoc.CreateElement("Input");
                data.AppendChild(nodeEl);

                _WriteVariables(InOutMemory.InputMemory, nodeEl);
            }
            if (InOutMemory.OutputMemory.Datas.Count > 0)
            {
                XmlElement nodeEl = xmlDoc.CreateElement("Output");
                data.AppendChild(nodeEl);

                _WriteVariables(InOutMemory.OutputMemory, nodeEl);
            }
        }

        protected override bool _OnCheckValid()
        {
            bool bRes = true;
            foreach (var v in m_InOutMemory.InputMemory.Datas)
            {
                if (!v.Variable.CheckValid())
                    bRes = false; ;
            }
            foreach (var v in m_InOutMemory.OutputMemory.Datas)
            {
                if (!v.Variable.CheckValid())
                    bRes = false; ;
            }
            return bRes;
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

        protected static string s_Icon = "▶";
        public override string Icon => m_Icon;
        protected string m_Icon = s_Icon;
        public void SetIcon(string icon) { m_Icon = icon; }

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
            node.m_Icon = this.m_Icon;

            return node;
        }
        public override string Note
        {
            get
            {
                string[] values = new string[m_Variables.Datas.Count];
                int i = 0;
                foreach (var v in m_Variables.Datas)
                {
                    values[i++] = v.Variable.NoteValue;
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
            Variable optr = NodeMemory.CreateVariable(
                "Operator",
                "ADD",
                Variable.CreateParams_Enum,
                Variable.CountType.CT_SINGLE,
                Variable.VariableType.VBT_Const,
                0,
                "ADD|SUB|MUL|DIV"
            );

            Variable opl = NodeMemory.CreateVariable(
                "Opl",
                "0",
                Variable.CreateParams_CalculatorTypes,
                Variable.CountType.CT_SINGLE,
                Variable.VariableType.VBT_Pointer,
                1
            );

            Variable opr1 = NodeMemory.CreateVariable(
                "Opr1",
                "0",
                Variable.CreateParams_CalculatorTypes,
                Variable.CountType.CT_SINGLE,
                Variable.VariableType.VBT_NONE,
                1
            );

            Variable opr2 = NodeMemory.CreateVariable(
                "Opr2",
                "0",
                Variable.CreateParams_CalculatorTypes,
                Variable.CountType.CT_SINGLE,
                Variable.VariableType.VBT_NONE,
                1
            );
        }

        public override string Note
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("{0} << {1} {2} {3}",
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
            Variable optr = NodeMemory.CreateVariable(
                "Operator",
                "==",
                Variable.CreateParams_Enum,
                Variable.CountType.CT_SINGLE,
                Variable.VariableType.VBT_Const,
                0,
                "==|!=|>|<|>=|<="
            );

            Variable opl = NodeMemory.CreateVariable(
                "Opl",
                "0",
                Variable.CreateParams_AllTypes,
                Variable.CountType.CT_SINGLE,
                Variable.VariableType.VBT_Pointer,
                1
            );

            Variable opr = NodeMemory.CreateVariable(
                "Opr",
                "0",
                Variable.CreateParams_AllTypes,
                Variable.CountType.CT_SINGLE,
                Variable.VariableType.VBT_NONE,
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
        public override string Icon => "x<<y";

        public SetDataNode()
        {
            m_Name = "SetData";
            m_Type = NodeType.NT_Default;
            m_Hierachy = NodeHierachy.NH_DefaultAction;

        }

        public override void CreateVariables()
        {
            Variable opl = NodeMemory.CreateVariable(
                "Target",
                "0",
                Variable.CreateParams_AllTypes,
                Variable.CountType.CT_NONE,
                Variable.VariableType.VBT_Pointer,
                1
            );

            Variable opr = NodeMemory.CreateVariable(
                "Source",
                "0",
                Variable.CreateParams_AllTypes,
                Variable.CountType.CT_NONE,
                Variable.VariableType.VBT_NONE,
                1
            );
        }

        public override string Note
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("{0} << {1}",
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
            Variable opl = NodeMemory.CreateVariable(
                "Target",
                "0",
                Variable.CreateParams_RandomTypes,
                Variable.CountType.CT_SINGLE,
                Variable.VariableType.VBT_Pointer,
                1
            );

            Variable opr1 = NodeMemory.CreateVariable(
                "Bound1",
                "0",
                Variable.CreateParams_RandomTypes,
                Variable.CountType.CT_SINGLE,
                Variable.VariableType.VBT_NONE,
                1
            );

            Variable opr2 = NodeMemory.CreateVariable(
                "Bound2",
                "0",
                Variable.CreateParams_RandomTypes,
                Variable.CountType.CT_SINGLE,
                Variable.VariableType.VBT_NONE,
                1
            );
        }

        public override string Note
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("{0} << [ {1} ~ {2} )",
                    Variables.GetVariable("Target").NoteValue,
                    Variables.GetVariable("Bound1").NoteValue,
                    Variables.GetVariable("Bound2").NoteValue);
                return sb.ToString();
            }
        }
    }

    class RandomSelectNode : LeafNode
    {
        public override string Icon => "～";

        public RandomSelectNode()
        {
            m_Name = "RandomSelect";
            m_Type = NodeType.NT_Default;
            m_Hierachy = NodeHierachy.NH_DefaultAction;

        }

        public override void CreateVariables()
        {
            Variable opl = NodeMemory.CreateVariable(
                "Input",
                "",
                Variable.CreateParams_AllTypes,
                Variable.CountType.CT_LIST,
                Variable.VariableType.VBT_NONE,
                1
            );

            Variable opr1 = NodeMemory.CreateVariable(
                "Output",
                "",
                Variable.CreateParams_AllTypes,
                Variable.CountType.CT_SINGLE,
                Variable.VariableType.VBT_Pointer,
                1
            );
        }

        public override string Note
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("{1} <~= {0}",
                    Variables.GetVariable("Input").NoteValue,
                    Variables.GetVariable("Output").NoteValue);
                return sb.ToString();
            }
        }
    }

    class ReadRegisterNode : LeafNode
    {
        public override string Icon => "[_↓_]";

        public ReadRegisterNode()
        {
            m_Name = "ReadRegister";
            m_Type = NodeType.NT_Default;
            m_Hierachy = NodeHierachy.NH_DefaultAction;

        }

        public override void CreateVariables()
        {
            Variable name = NodeMemory.CreateVariable(
                "Event",
                "",
                Variable.CreateParams_String,
                Variable.CountType.CT_SINGLE,
                Variable.VariableType.VBT_Pointer,
                0
            );

            Variable ints = NodeMemory.CreateVariable(
                "Int",
                "",
                Variable.CreateParams_Int,
                Variable.CountType.CT_LIST,
                Variable.VariableType.VBT_Pointer,
                0
            );

            Variable floats = NodeMemory.CreateVariable(
                "Float",
                "",
                Variable.CreateParams_Float,
                Variable.CountType.CT_LIST,
                Variable.VariableType.VBT_Pointer,
                0
            );

            Variable strings = NodeMemory.CreateVariable(
                "String",
                "",
                Variable.CreateParams_String,
                Variable.CountType.CT_LIST,
                Variable.VariableType.VBT_Pointer,
                0
            );

            Variable ulongs = NodeMemory.CreateVariable(
                "Ulong",
                "",
                Variable.CreateParams_Ulong,
                Variable.CountType.CT_LIST,
                Variable.VariableType.VBT_Pointer,
                0
            );

            Variable bools = NodeMemory.CreateVariable(
                "Bool",
                "",
                Variable.CreateParams_Bool,
                Variable.CountType.CT_LIST,
                Variable.VariableType.VBT_Pointer,
                0
            );
        }

        public override string Note
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("{0}\nInt: {1}\nFloat: {2}\nString: {3}\nUlong: {4}\nBool: {5}",
                    Variables.GetVariable("Event").NoteValue,
                    Variables.GetVariable("Int").NoteValue,
                    Variables.GetVariable("Float").NoteValue,
                    Variables.GetVariable("String").NoteValue,
                    Variables.GetVariable("Ulong").NoteValue,
                    Variables.GetVariable("Bool").NoteValue);
                return sb.ToString();
            }
        }
    }


    class WriteRegisterNode : LeafNode
    {
        public override string Icon => "[_↑_]";

        public WriteRegisterNode()
        {
            m_Name = "WriteRegister";
            m_Type = NodeType.NT_Default;
            m_Hierachy = NodeHierachy.NH_DefaultAction;

        }

        public override void CreateVariables()
        {
            Variable name = NodeMemory.CreateVariable(
                "Event",
                "",
                Variable.CreateParams_String,
                Variable.CountType.CT_SINGLE,
                Variable.VariableType.VBT_NONE,
                0
            );

            Variable ints = NodeMemory.CreateVariable(
                "Int",
                "",
                Variable.CreateParams_Int,
                Variable.CountType.CT_LIST,
                Variable.VariableType.VBT_NONE,
                0
            );

            Variable floats = NodeMemory.CreateVariable(
                "Float",
                "",
                Variable.CreateParams_Float,
                Variable.CountType.CT_LIST,
                Variable.VariableType.VBT_NONE,
                0
            );

            Variable strings = NodeMemory.CreateVariable(
                "String",
                "",
                Variable.CreateParams_String,
                Variable.CountType.CT_LIST,
                Variable.VariableType.VBT_NONE,
                0
            );

            Variable ulongs = NodeMemory.CreateVariable(
                "Ulong",
                "",
                Variable.CreateParams_Ulong,
                Variable.CountType.CT_LIST,
                Variable.VariableType.VBT_NONE,
                0
            );

            Variable bools = NodeMemory.CreateVariable(
                "Bool",
                "",
                Variable.CreateParams_Bool,
                Variable.CountType.CT_LIST,
                Variable.VariableType.VBT_NONE,
                0
            );
        }

        public override string Note
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("{0}\nInt: {1}\nFloat: {2}\nString: {3}\nUlong: {4}\nBool: {5}",
                    Variables.GetVariable("Event").NoteValue,
                    Variables.GetVariable("Int").NoteValue,
                    Variables.GetVariable("Float").NoteValue,
                    Variables.GetVariable("String").NoteValue,
                    Variables.GetVariable("Ulong").NoteValue,
                    Variables.GetVariable("Bool").NoteValue);
                return sb.ToString();
            }
        }
    }

    class SwitchCaseNode : CompositeNode
    {
        public override string Icon => "↙↓↘";

        public SwitchCaseNode()
        {
            m_Name = "SwitchCase";
            m_Type = NodeType.NT_Default;
            m_Hierachy = NodeHierachy.NH_Compositor;

            new ConnectionSingle(this, Connection.IdentifierDefault);
        }

        public override void CreateVariables()
        {
            NodeMemory.CreateVariable(
                "Switch",
                "0",
                Variable.CreateParams_SwitchTypes,
                Variable.CountType.CT_SINGLE,
                Variable.VariableType.VBT_Pointer,
                1
            );

            NodeMemory.CreateVariable(
                "Cases",
                "",
                Variable.CreateParams_SwitchTypes,
                Variable.CountType.CT_LIST,
                Variable.VariableType.VBT_NONE,
                1
            );
        }

        public override string Note
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("{0} from {{ {1} }}",
                    Variables.GetVariable("Switch").NoteValue,
                    Variables.GetVariable("Cases").NoteValue);
                return sb.ToString();
            }
        }
    }

    class ForNode : BranchNode
    {
        public override string Icon => "↺";

        public ForNode()
        {
            m_Name = "For";
            m_Type = NodeType.NT_Default;
            m_Hierachy = NodeHierachy.NH_Compositor;

            new ConnectionSingle(this, Connection.IdentifierInit);
            new ConnectionSingle(this, Connection.IdentifierCond);
            new ConnectionSingle(this, Connection.IdentifierIncrement);

            ///> to make the "chilren" conn the last conn
            m_ChildConn = new ConnectionSingle(this, Connection.IdentifierChildren);
        }

        public override void CreateVariables()
        {
            NodeMemory.CreateVariable(
                "ExitWhenFailure",
                "F",
                Variable.CreateParams_Bool,
                Variable.CountType.CT_SINGLE,
                Variable.VariableType.VBT_NONE
            );
        }
    }

    class ForEachNode : SingleChildNode
    {
        public override string Icon => "↺";

        public ForEachNode()
        {
            m_Name = "ForEach";
            m_Type = NodeType.NT_Default;
            m_Hierachy = NodeHierachy.NH_Compositor;
        }

        public override void CreateVariables()
        {
            NodeMemory.CreateVariable(
                "Collection",
                "",
                Variable.CreateParams_AllTypes,
                Variable.CountType.CT_LIST,
                Variable.VariableType.VBT_NONE,
                1
            );
            NodeMemory.CreateVariable(
                "Current",
                "",
                Variable.CreateParams_AllTypes,
                Variable.CountType.CT_SINGLE,
                Variable.VariableType.VBT_Pointer,
                1
            );
            NodeMemory.CreateVariable(
                "ExitWhenFailure",
                "F",
                Variable.CreateParams_Bool,
                Variable.CountType.CT_SINGLE,
                Variable.VariableType.VBT_NONE
            );
        }

        public override string Note
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("{0} from {{ {1} }}",
                    Variables.GetVariable("Current").NoteValue,
                    Variables.GetVariable("Collection").NoteValue);
                return sb.ToString();
            }
        }
    }

    class LoopNode : SingleChildNode
    {
        public override string Icon => "↺";

        public LoopNode()
        {
            m_Name = "Loop";
            m_Type = NodeType.NT_Default;
            m_Hierachy = NodeHierachy.NH_Compositor;
        }

        public override void CreateVariables()
        {
            NodeMemory.CreateVariable(
                "Count",
                "",
                Variable.CreateParams_Int,
                Variable.CountType.CT_SINGLE,
                Variable.VariableType.VBT_NONE,
                1
            );
            NodeMemory.CreateVariable(
                "Current",
                "",
                Variable.CreateParams_Int,
                Variable.CountType.CT_SINGLE,
                Variable.VariableType.VBT_Pointer,
                1
            );
            NodeMemory.CreateVariable(
                "ExitWhenFailure",
                "F",
                Variable.CreateParams_Bool,
                Variable.CountType.CT_SINGLE,
                Variable.VariableType.VBT_NONE
            );
        }

        public override string Note
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("{0} at [0, {1})",
                    Variables.GetVariable("Current").NoteValue,
                    Variables.GetVariable("Count").NoteValue);
                return sb.ToString();
            }
        }
    }

    class PiecewiseFunctionNode : LeafNode
    {
        public override string Icon => "_|￣";

        public PiecewiseFunctionNode()
        {
            m_Name = "PiecewiseFunction";
            m_Type = NodeType.NT_Default;
            m_Hierachy = NodeHierachy.NH_DefaultAction;
        }

        public override void CreateVariables()
        {
            NodeMemory.CreateVariable(
                "KeyPointX",
                "",
                Variable.CreateParams_AllNumbers,
                Variable.CountType.CT_LIST,
                Variable.VariableType.VBT_NONE,
                1
            );

            NodeMemory.CreateVariable(
                "KeyPointY",
                "",
                Variable.CreateParams_AllNumbers,
                Variable.CountType.CT_LIST,
                Variable.VariableType.VBT_NONE,
                1
            );

            NodeMemory.CreateVariable(
                "InputX",
                "",
                Variable.CreateParams_AllNumbers,
                Variable.CountType.CT_SINGLE,
                Variable.VariableType.VBT_NONE,
                1
            );

            NodeMemory.CreateVariable(
                "OutputY",
                "",
                Variable.CreateParams_AllNumbers,
                Variable.CountType.CT_SINGLE,
                Variable.VariableType.VBT_Pointer,
                1
            );

        }

        public override string Note
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("{0} << func({1})",
                    Variables.GetVariable("OutputY").NoteValue,
                    Variables.GetVariable("InputX").NoteValue);
                return sb.ToString();
            }
        }
    }

    class DiceNode : LeafNode
    {
        public override string Icon => "🎲";

        public DiceNode()
        {
            m_Name = "Dice";
            m_Type = NodeType.NT_Default;
            m_Hierachy = NodeHierachy.NH_DefaultAction;
        }

        public override void CreateVariables()
        {
            NodeMemory.CreateVariable(
                "Distribution",
                "",
                Variable.CreateParams_AllNumbers,
                Variable.CountType.CT_LIST,
                Variable.VariableType.VBT_NONE,
                1
            );

            NodeMemory.CreateVariable(
                "Values",
                "",
                Variable.CreateParams_AllTypes,
                Variable.CountType.CT_LIST,
                Variable.VariableType.VBT_NONE,
                2
            );

            NodeMemory.CreateVariable(
                "Input",
                "0",
                Variable.CreateParams_AllNumbers,
                Variable.CountType.CT_SINGLE,
                Variable.VariableType.VBT_NONE,
                1
            );

            NodeMemory.CreateVariable(
                "Output",
                "",
                Variable.CreateParams_AllTypes,
                Variable.CountType.CT_SINGLE,
                Variable.VariableType.VBT_Pointer,
                2
            );

            NodeMemory.CreateVariable(
                "IgnoreInput",
                "T",
                Variable.CreateParams_Bool,
                Variable.CountType.CT_SINGLE,
                Variable.VariableType.VBT_NONE,
                0
            );

        }

        public override string Note
        {
            get
            {
                return string.Empty;
                //StringBuilder sb = new StringBuilder();
                //sb.AppendFormat("{0} << func({1})",
                //    Variables.GetVariable("OutputY").NoteValue,
                //    Variables.GetVariable("InputX").NoteValue);
                //return sb.ToString();
            }
        }
    }

    public class SubTreeNode : LeafNode
    {
        public override string Icon => "♣";

        InOutMemory m_InOutMemory;
        public InOutMemory InOutMemory { get { return m_InOutMemory; } }
        public override InOutMemory InOutData => m_InOutMemory;
        Variable m_Tree;
        string m_LoadedTree = null;

        protected override void _OnCreateBase()
        {
            m_InOutMemory = new InOutMemory(this, false);
        }

        public SubTreeNode()
        {
            m_Name = "SubTree";
            m_Type = NodeType.NT_Default;
            m_Hierachy = NodeHierachy.NH_DefaultAction;
        }

        public override void CreateVariables()
        {
            m_Tree = NodeMemory.CreateVariable(
                "Tree",
                "",
                Variable.CreateParams_String,
                Variable.CountType.CT_SINGLE,
                Variable.VariableType.VBT_Const
            );

            NodeMemory.CreateVariable(
                "Identification",
                "",
                Variable.CreateParams_String,
                Variable.CountType.CT_SINGLE,
                Variable.VariableType.VBT_Const
            );
        }

        public override string Note
        {
            get
            {
                string s = Variables.GetVariable("Identification").NoteValue;
                if (string.IsNullOrEmpty(s))
                    return Variables.GetVariable("Tree").NoteValue;
                return s;
            }
        }
        protected override void _OnLoaded()
        {
            m_LoadedTree = m_Tree.Value;
        }

        public bool LoadInOut()
        {
            if (m_LoadedTree != null && m_LoadedTree != m_Tree.Value)
            {
                using (var locker = WorkBenchMgr.Instance.CommandLocker.StartLock())
                {
                    InOutMemory source = InOutMemoryMgr.Instance.Get(m_Tree.Value);
                    if (source == null)
                        return false;
                    source.CloneTo(m_InOutMemory);
                }
                m_LoadedTree = m_Tree.Value;
                return true;
            }
            return false;
        }

        public bool ReloadInOut()
        {
            using (var locker = WorkBenchMgr.Instance.CommandLocker.StartLock())
            {
                InOutMemory source = InOutMemoryMgr.Instance.Reload(m_Tree.Value);
                if (source == null)
                    return false;
                m_InOutMemory.DiffReplaceBy(source);
            }
            m_LoadedTree = m_Tree.Value;
            return true;
        }

        protected override void _OnLoadChild(XmlNode data)
        {
            if (data.Name == "Input" || data.Name == "Output")
            {
                foreach (System.Xml.XmlAttribute attr in data.Attributes)
                {
                    if (ReservedAttributes.Contains(attr.Name))
                        continue;

                    if (!m_InOutMemory.TryAddData(attr.Name, attr.Value, data.Name == "Input"))
                    {
                        LogMgr.Instance.Error("Error when add Input/Output: " + attr.Name + " " + attr.Value);
                        continue;
                    }
                }
            }
        }

        protected override void _OnSaveVariables(XmlElement data, XmlDocument xmlDoc)
        {
            _WriteVariables(Variables, data);
            _WriteMemory(data, xmlDoc);
        }
        protected override void _OnExportVariables(XmlElement data, XmlDocument xmlDoc)
        {
            _WriteVariables(Variables, data);
            _WriteMemory(data, xmlDoc);
        }

        void _WriteMemory(XmlElement data, XmlDocument xmlDoc)
        {
            if (InOutMemory.InputMemory.Datas.Count > 0)
            {
                XmlElement nodeEl = xmlDoc.CreateElement("Input");
                data.AppendChild(nodeEl);

                _WriteVariables(InOutMemory.InputMemory, nodeEl);
            }
            if (InOutMemory.OutputMemory.Datas.Count > 0)
            {
                XmlElement nodeEl = xmlDoc.CreateElement("Output");
                data.AppendChild(nodeEl);

                _WriteVariables(InOutMemory.OutputMemory, nodeEl);
            }
        }

        protected override bool _OnCheckValid()
        {
            bool bRes = true;
            foreach (var v in m_InOutMemory.InputMemory.Datas)
            {
                if (!v.Variable.CheckValid())
                    bRes = false; ;
            }
            foreach (var v in m_InOutMemory.OutputMemory.Datas)
            {
                if (!v.Variable.CheckValid())
                    bRes = false; ;
            }
            return bRes;
        }
    }


    class WaitNode : LeafNode
    {
        public override string Icon => "⏰";

        public WaitNode()
        {
            m_Name = "Wait";
            m_Type = NodeType.NT_Default;
            m_Hierachy = NodeHierachy.NH_DefaultAction;
        }

        public override void CreateVariables()
        {
            NodeMemory.CreateVariable(
                "TickCount",
                "1",
                Variable.CreateParams_Int,
                Variable.CountType.CT_SINGLE,
                Variable.VariableType.VBT_NONE
            );
        }

        public override string Note
        {
            get
            {
                return Variables.GetVariable("TickCount").NoteValue;
            }
        }
    }

    class ClearArrayNode : LeafNode
    {
        public override string Icon => "[ ]";

        public ClearArrayNode()
        {
            m_Name = "ClearArray";
            m_Type = NodeType.NT_Default;
            m_Hierachy = NodeHierachy.NH_Array;

        }

        public override void CreateVariables()
        {
            Variable opl = NodeMemory.CreateVariable(
                "Array",
                "",
                Variable.CreateParams_AllTypes,
                Variable.CountType.CT_LIST,
                Variable.VariableType.VBT_Pointer
            );
        }

        public override string Note
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("{0}",
                    Variables.GetVariable("Array").NoteValue
                    );
                return sb.ToString();
            }
        }
    }

    class GetArrayLengthNode : LeafNode
    {
        public override string Icon => "[].Length";

        public GetArrayLengthNode()
        {
            m_Name = "GetArrayLength";
            m_Type = NodeType.NT_Default;
            m_Hierachy = NodeHierachy.NH_Array;

        }

        public override void CreateVariables()
        {
            Variable array = NodeMemory.CreateVariable(
                "Array",
                "",
                Variable.CreateParams_AllTypes,
                Variable.CountType.CT_LIST,
                Variable.VariableType.VBT_NONE
            );

            Variable length = NodeMemory.CreateVariable(
                "Length",
                "0",
                Variable.CreateParams_Int,
                Variable.CountType.CT_SINGLE,
                Variable.VariableType.VBT_Pointer
            );
        }

        public override string Note
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("{0} << [{1}].Length",
                    Variables.GetVariable("Length").NoteValue,
                    Variables.GetVariable("Array").NoteValue
                    );
                return sb.ToString();
            }
        }
    }

    class ArrayPushElementNode : LeafNode
    {
        public override string Icon => "[x] <-- y";

        public ArrayPushElementNode()
        {
            m_Name = "ArrayPushElement";
            m_Type = NodeType.NT_Default;
            m_Hierachy = NodeHierachy.NH_Array;

        }

        public override void CreateVariables()
        {
            Variable array = NodeMemory.CreateVariable(
                "Array",
                "",
                Variable.CreateParams_AllTypes,
                Variable.CountType.CT_LIST,
                Variable.VariableType.VBT_Pointer,
                1
            );

            Variable element = NodeMemory.CreateVariable(
                "Element",
                "0",
                Variable.CreateParams_AllTypes,
                Variable.CountType.CT_SINGLE,
                Variable.VariableType.VBT_NONE,
                1
            );
        }

        public override string Note
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("[{0}] add {1}",
                    Variables.GetVariable("Array").NoteValue,
                    Variables.GetVariable("Element").NoteValue
                    );
                return sb.ToString();
            }
        }
    }

    class ShuffleNode : LeafNode
    {
        public override string Icon => "[x]<??<[y]";

        public ShuffleNode()
        {
            m_Name = "Shuffle";
            m_Type = NodeType.NT_Default;
            m_Hierachy = NodeHierachy.NH_DefaultAction;

        }

        public override void CreateVariables()
        {
            Variable input = NodeMemory.CreateVariable(
                "Input",
                "",
                Variable.CreateParams_AllTypes,
                Variable.CountType.CT_LIST,
                Variable.VariableType.VBT_NONE,
                1
            );

            Variable output = NodeMemory.CreateVariable(
                "Output",
                "",
                Variable.CreateParams_AllTypes,
                Variable.CountType.CT_LIST,
                Variable.VariableType.VBT_Pointer,
                1
            );
        }

        public override string Note
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("[{0}] <??< [{1}]",
                    Variables.GetVariable("Output").NoteValue,
                    Variables.GetVariable("Input").NoteValue
                    );
                return sb.ToString();
            }
        }
    }

}