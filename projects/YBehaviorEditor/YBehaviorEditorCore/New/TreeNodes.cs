using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Xml;

namespace YBehavior.Editor.Core.New
{
    public class RootTreeNode : SingleChildNode
    {
        public RootTreeNode() : base()
        {
            m_Name = "Root";
            Type = TreeNodeType.TNT_Root;
        }

        protected override void _OnLoadChild(XmlNode data)
        {
            if (data.Name == "Shared" || data.Name == "Local")
            {
                foreach (System.Xml.XmlAttribute attr in data.Attributes)
                {
                    if (ReservedAttributes.Contains(attr.Name))
                        continue;
                    Tree.SharedData.TryAddData(attr.Name, attr.Value);
                }
            }
            else if (data.Name == "Input" || data.Name == "Output")
            {
                foreach (System.Xml.XmlAttribute attr in data.Attributes)
                {
                    if (ReservedAttributes.Contains(attr.Name))
                        continue;

                    if (!Tree.InOutMemory.TryAddData(attr.Name, attr.Value, data.Name == "Input"))
                    {
                        LogMgr.Instance.Error("Error when add Input/Output: " + attr.Name + " " + attr.Value);
                        continue;
                    }
                }
            }
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
            if (Tree.SharedData.SharedMemory.Datas.Count > 0)
            {
                XmlElement nodeEl = xmlDoc.CreateElement("Shared");
                data.AppendChild(nodeEl);

                _WriteVariables(Tree.SharedData.SharedMemory, nodeEl);
            }
            if (Tree.SharedData.LocalMemory.Datas.Count > 0)
            {
                XmlElement nodeEl = xmlDoc.CreateElement("Local");
                data.AppendChild(nodeEl);

                _WriteVariables(Tree.SharedData.LocalMemory, nodeEl);
            }
            if (Tree.InOutMemory.InputMemory.Datas.Count > 0)
            {
                XmlElement nodeEl = xmlDoc.CreateElement("Input");
                data.AppendChild(nodeEl);

                _WriteVariables(Tree.InOutMemory.InputMemory, nodeEl);
            }
            if (Tree.InOutMemory.OutputMemory.Datas.Count > 0)
            {
                XmlElement nodeEl = xmlDoc.CreateElement("Output");
                data.AppendChild(nodeEl);

                _WriteVariables(Tree.InOutMemory.OutputMemory, nodeEl);
            }
        }

        protected override bool _OnCheckValid()
        {
            bool bRes = true;
            foreach (var v in Tree.InOutMemory.InputMemory.Datas)
            {
                if (!v.Variable.CheckValid())
                    bRes = false; ;
            }
            foreach (var v in Tree.InOutMemory.OutputMemory.Datas)
            {
                if (!v.Variable.CheckValid())
                    bRes = false; ;
            }
            return bRes;
        }

    }

    public class BranchNode : TreeNode
    {
        protected Connector m_Ctr;
        public Connector Ctr { get { return m_Ctr; } }
    }

    public class LeafNode : TreeNode
    {
        public LeafNode()
        {
        }
    }

    public class SingleChildNode : BranchNode
    {
        public SingleChildNode()
        {
            m_Ctr = m_Connections.Add(Connector.IdentifierChildren, false);
        }
    }

    public class CompositeNode : BranchNode
    {
        public CompositeNode()
        {
            m_Ctr = m_Connections.Add(Connector.IdentifierChildren, true);
        }
    }

    public class ActionTreeNode : LeafNode
    {
        public string NoteFormat { get; set; }
        public string ClassName { get; set; }
        public override string Name => ClassName;

        protected static string s_Icon = "▶";
        public override string Icon => m_Icon;
        protected string m_Icon = s_Icon;
        public void SetIcon(string icon) { m_Icon = icon; }

        public ActionTreeNode() : base()
        {
            Type = TreeNodeType.TNT_Invalid;
        }

        public override NodeBase Clone()
        {
            ActionTreeNode node = base.Clone() as ActionTreeNode;
            node.NoteFormat = this.NoteFormat;
            node.ClassName = this.ClassName;
            node.m_Icon = this.m_Icon;

            return node;
        }
    }

    public class SequenceTreeNode : CompositeNode
    {
        public SequenceTreeNode() : base()
        {
            m_Name = "Sequence";
            Type = TreeNodeType.TNT_Default;
        }
        public override string Icon => "➜➜➜";
    }
    class SelectorTreeNode : CompositeNode
    {
        public SelectorTreeNode()
        {
            m_Name = "Selector";
            Type = TreeNodeType.TNT_Default;
        }

        public override string Icon => "？？？";
    }

    class RandomSequenceTreeNode : CompositeNode
    {
        public RandomSequenceTreeNode()
        {
            m_Name = "RandomSequence";
            Type = TreeNodeType.TNT_Default;
        }

        public override string Icon => "～➜➜➜";
    }
    class RandomSelectorTreeNode : CompositeNode
    {
        public RandomSelectorTreeNode()
        {
            m_Name = "RandomSelector";
            Type = TreeNodeType.TNT_Default;
        }

        public override string Icon => "～？？？";
    }

    public class SubTreeNode : LeafNode
    {
        public class SubTreeNodeWrapper : TreeNodeWrapper
        {
            public new InOutMemory InOutData { get { return (Node as SubTreeNode).InOutMemory; } }
        };
        public class TreeVariable : Variable
        {
            public TreeVariable(IVariableDataSource source) : base(source)
            { }
            public DelayableNotificationCollection<string> TreeList { get { return FileMgr.Instance.TreeList; } }
        }
        InOutMemory m_InOutMemory;
        public InOutMemory InOutMemory { get { return m_InOutMemory; } }

        Variable m_Tree;
        string m_LoadedTree = null;

        public SubTreeNode() : base()
        {
            m_Name = "SubTree";
            Type = TreeNodeType.TNT_Default;
        }

        protected override void _CreateWrapper()
        {
            m_Wrapper = new SubTreeNodeWrapper();
        }
        protected override void _OnCreateBase()
        {
            m_InOutMemory = new InOutMemory(m_Wrapper as SubTreeNodeWrapper, false);
        }

        public override void CreateVariables()
        {
            m_Tree = new TreeVariable(NodeMemory.Owner);
            NodeMemory.CreateVariable(m_Tree,
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
                    m_InOutMemory.CloneFrom(source);
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
            base._OnLoadChild(data);
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

    class CalculatorTreeNode : LeafNode
    {
        public override string Icon => "+-×÷";
        public CalculatorTreeNode()
        {
            m_Name = "Calculator";
            Type = TreeNodeType.TNT_Default;

        }

        public override void CreateVariables()
        {
            Variable optr = NodeMemory.CreateVariable(
                "Operator",
                "+",
                Variable.CreateParams_Enum,
                Variable.CountType.CT_SINGLE,
                Variable.VariableType.VBT_Const,
                0,
                "+|-|*|/"
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
                    Variables.GetVariable("Operator").NoteValue,
                    Variables.GetVariable("Opr2").NoteValue);
                return sb.ToString();
            }
        }
    }

    class AlwaysSuccessTreeNode : LeafNode
    {
        public override string Icon => "T";

        public AlwaysSuccessTreeNode()
        {
            m_Name = "AlwaysSuccess";
            Type = TreeNodeType.TNT_Default;
        }
    }

    class AlwaysFailureTreeNode : LeafNode
    {
        public override string Icon => "F";

        public AlwaysFailureTreeNode()
        {
            m_Name = "AlwaysFailure";
            Type = TreeNodeType.TNT_Default;
        }
    }

    class InvertorTreeNode : LeafNode
    {
        public override string Icon => "!";

        public InvertorTreeNode()
        {
            m_Name = "Invertor";
            Type = TreeNodeType.TNT_Default;
        }
    }

    class ComparerTreeNode : LeafNode
    {
        //static Dictionary<string, string> s_OperatorDic = new Dictionary<string, string>() { { "ADD", "+" }, { "SUB", "-" }, { "MUL", "*" }, { "DIV", "/" } };
        public override string Icon => "x ？y";

        public ComparerTreeNode()
        {
            m_Name = "Comparer";
            Type = TreeNodeType.TNT_Default;
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

    class SetDataTreeNode : LeafNode
    {
        public override string Icon => "x<<y";

        public SetDataTreeNode()
        {
            m_Name = "SetData";
            Type = TreeNodeType.TNT_Default;
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

    class IfThenElseTreeNode : TreeNode
    {
        public override string Icon => "↙ ？↘";

        public IfThenElseTreeNode()
        {
            m_Name = "IfThenElse";
            Type = TreeNodeType.TNT_Default;

            m_Connections.Add("if", false);
            m_Connections.Add("then", false);
            m_Connections.Add("else", false);
        }
    }

    class RandomTreeNode : LeafNode
    {
        public override string Icon => "～";

        public RandomTreeNode()
        {
            m_Name = "Random";
            Type = TreeNodeType.TNT_Default;

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

    class RandomSelectTreeNode : LeafNode
    {
        public override string Icon => "～";

        public RandomSelectTreeNode()
        {
            m_Name = "RandomSelect";
            Type = TreeNodeType.TNT_Default;
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

    class ReadRegisterTreeNode : LeafNode
    {
        public override string Icon => "[_↓_]";

        public ReadRegisterTreeNode()
        {
            m_Name = "ReadRegister";
            Type = TreeNodeType.TNT_Default;
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
    class WriteRegisterTreeNode : LeafNode
    {
        public override string Icon => "[_↑_]";

        public WriteRegisterTreeNode()
        {
            m_Name = "WriteRegister";
            Type = TreeNodeType.TNT_Default;
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

    class SwitchCaseTreeNode : CompositeNode
    {
        public override string Icon => "↙↓↘";

        public SwitchCaseTreeNode()
        {
            m_Name = "SwitchCase";
            Type = TreeNodeType.TNT_Default;

            m_Connections.Add(Connector.IdentifierDefault, false);
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

    class ForTreeNode : BranchNode
    {
        public override string Icon => "↺";

        public ForTreeNode()
        {
            m_Name = "For";
            Type = TreeNodeType.TNT_Default;

            m_Connections.Add(Connector.IdentifierInit, false);
            m_Connections.Add(Connector.IdentifierCond, false);
            m_Connections.Add(Connector.IdentifierIncrement, false);

            ///> to make the "chilren" conn the last conn
            m_Ctr = m_Connections.Add(Connector.IdentifierChildren, false);
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

    class ForEachTreeNode : SingleChildNode
    {
        public override string Icon => "↺";

        public ForEachTreeNode()
        {
            m_Name = "ForEach";
            Type = TreeNodeType.TNT_Default;
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

    class LoopTreeNode : SingleChildNode
    {
        public override string Icon => "↺";

        public LoopTreeNode()
        {
            m_Name = "Loop";
            Type = TreeNodeType.TNT_Default;
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

    class PiecewiseFunctionTreeNode : LeafNode
    {
        public override string Icon => "_|￣";

        public PiecewiseFunctionTreeNode()
        {
            m_Name = "PiecewiseFunction";
            Type = TreeNodeType.TNT_Default;
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

    class DiceTreeNode : LeafNode
    {
        public override string Icon => "🎲";

        public DiceTreeNode()
        {
            m_Name = "Dice";
            Type = TreeNodeType.TNT_Default;
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

    class WaitTreeNode : LeafNode
    {
        public override string Icon => "⏰";

        public WaitTreeNode()
        {
            m_Name = "Wait";
            Type = TreeNodeType.TNT_Default;
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

    class ClearArrayTreeNode : LeafNode
    {
        public override string Icon => "[ ]";

        public ClearArrayTreeNode()
        {
            m_Name = "ClearArray";
            Type = TreeNodeType.TNT_Default;
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

    class IsArrayEmptyTreeNode : LeafNode
    {
        public override string Icon => "[ ]";

        public IsArrayEmptyTreeNode()
        {
            m_Name = "IsArrayEmpty";
            Type = TreeNodeType.TNT_Default;
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

    class GetArrayLengthTreeNode : LeafNode
    {
        public override string Icon => "[].Length";

        public GetArrayLengthTreeNode()
        {
            m_Name = "GetArrayLength";
            Type = TreeNodeType.TNT_Default;
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

    class ArrayPushElementTreeNode : LeafNode
    {
        public override string Icon => "[x] <-- y";

        public ArrayPushElementTreeNode()
        {
            m_Name = "ArrayPushElement";
            Type = TreeNodeType.TNT_Default;
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

    class ShuffleTreeNode : LeafNode
    {
        public override string Icon => "[x]<??<[y]";

        public ShuffleTreeNode()
        {
            m_Name = "Shuffle";
            Type = TreeNodeType.TNT_Default;
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

    class FSMSetConditionTreeNode : LeafNode
    {
        public FSMSetConditionTreeNode()
        {
            m_Name = "FSMSetCondition";
            Type = TreeNodeType.TNT_Default;
        }

        public override void CreateVariables()
        {
            Variable conditions = NodeMemory.CreateVariable(
                "Conditions",
                "",
                Variable.CreateParams_String,
                Variable.CountType.CT_NONE,
                Variable.VariableType.VBT_NONE,
                1
            );
            Variable optr = NodeMemory.CreateVariable(
                "Operator",
                "On",
                Variable.CreateParams_Enum,
                Variable.CountType.CT_SINGLE,
                Variable.VariableType.VBT_Const,
                0,
                "On|Off"
            );
        }

        public override string Note
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("Set Conditions {0}: {1}",
                    Variables.GetVariable("Operator").NoteValue,
                    Variables.GetVariable("Conditions").NoteValue
                    );
                return sb.ToString();
            }
        }
    }

    class FSMClearConditionsTreeNode : LeafNode
    {
        public FSMClearConditionsTreeNode()
        {
            m_Name = "FSMClearConditions";
            Type = TreeNodeType.TNT_Default;
        }

        public override string Note
        {
            get
            {
                return "Clear Conditions.";
            }
        }
    }
}
