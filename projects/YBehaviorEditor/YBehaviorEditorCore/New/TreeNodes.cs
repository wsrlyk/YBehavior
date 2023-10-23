using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Xml;

namespace YBehavior.Editor.Core.New
{
    using ConvertType = KeyValuePair<Variable.ValueType, Variable.ValueType>;
    public class RootTreeNode : SingleChildNode
    {
        public RootTreeNode() : base()
        {
            m_Name = "Root";
            Type = TreeNodeType.TNT_Root;
            Geo.Pos = new Point(300, 100);
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
            _WriteMemory(data, xmlDoc, false);
        }
        protected override void _OnExportVariables(XmlElement data, XmlDocument xmlDoc)
        {
            _WriteMemory(data, xmlDoc, true);
        }

        void _WriteMemory(XmlElement data, XmlDocument xmlDoc, bool export)
        {
            if (Tree.SharedData.SharedMemory.Datas.Count > 0)
            {
                XmlElement nodeEl = xmlDoc.CreateElement("Shared");
                data.AppendChild(nodeEl);

                if (!export)
                    _SaveVariables(Tree.SharedData.SharedMemory, nodeEl);
                else
                    _ExportVariables(Tree.SharedData.SharedMemory, nodeEl, true);
            }
            if (Tree.SharedData.LocalMemory.Datas.Count > 0)
            {
                XmlElement nodeEl = xmlDoc.CreateElement("Local");
                data.AppendChild(nodeEl);

                if (!export)
                    _SaveVariables(Tree.SharedData.LocalMemory, nodeEl);
                else
                    _ExportVariables(Tree.SharedData.LocalMemory, nodeEl, true);
            }
            if (Tree.InOutMemory.InputMemory.Datas.Count > 0)
            {
                XmlElement nodeEl = xmlDoc.CreateElement("Input");
                data.AppendChild(nodeEl);

                if (!export)
                    _SaveVariables(Tree.InOutMemory.InputMemory, nodeEl);
                else
                    _ExportVariables(Tree.InOutMemory.InputMemory, nodeEl, false);
            }
            if (Tree.InOutMemory.OutputMemory.Datas.Count > 0)
            {
                XmlElement nodeEl = xmlDoc.CreateElement("Output");
                data.AppendChild(nodeEl);

                if (!export)
                    _SaveVariables(Tree.InOutMemory.OutputMemory, nodeEl);
                else
                    _ExportVariables(Tree.InOutMemory.OutputMemory, nodeEl, false);
            }
        }

        protected void _ExportVariables(IVariableCollection collection, System.Xml.XmlElement data, bool bOnlyActive)
        {
            foreach (VariableHolder v in collection.Datas)
            {
                if (v.Variable.eType == Variable.EnableType.ET_Disable)
                    continue;

                if (bOnlyActive && v.Variable.referencedType != Variable.ReferencedType.Active)
                    continue;

                data.SetAttribute(v.Variable.Name, v.Variable.ValueInXml);
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
            m_Ctr = m_Connections.Add(Connector.IdentifierChildren, false, Connector.PosType.CHILDREN);
        }
    }

    public class CompositeNode : BranchNode
    {
        public CompositeNode()
        {
            m_Ctr = m_Connections.Add(Connector.IdentifierChildren, true, Connector.PosType.CHILDREN);
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

        public override string Note
        {
            get
            {
                string[] values = new string[m_Variables.Datas.Count];
                int i = 0;
                foreach (var v in m_Variables.Datas)
                {
                    values[i++] = 
                        v.Variable.eType == Variable.EnableType.ET_Disable ? 
                        string.Empty : 
                        v.Variable.NoteValue;
                }
                return string.Format(NoteFormat, values);
            }
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
                Variable.VariableType.VBT_Const,
                Variable.EnableType.ET_FIXED
            );

            NodeMemory.CreateVariable(
                "Identification",
                "",
                Variable.CreateParams_String,
                Variable.CountType.CT_SINGLE,
                Variable.VariableType.VBT_Const,
                Variable.EnableType.ET_FIXED
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
            base._OnLoaded();
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
            _SaveVariables(Variables, data);
            _WriteMemory(data, xmlDoc);
        }
        protected override void _OnExportVariables(XmlElement data, XmlDocument xmlDoc)
        {
            _ExportVariables(Variables, data);
            _WriteMemory(data, xmlDoc);
        }

        void _WriteMemory(XmlElement data, XmlDocument xmlDoc)
        {
            if (InOutMemory.InputMemory.Datas.Count > 0)
            {
                XmlElement nodeEl = xmlDoc.CreateElement("Input");
                data.AppendChild(nodeEl);

                _SaveVariables(InOutMemory.InputMemory, nodeEl);
            }
            if (InOutMemory.OutputMemory.Datas.Count > 0)
            {
                XmlElement nodeEl = xmlDoc.CreateElement("Output");
                data.AppendChild(nodeEl);

                _SaveVariables(InOutMemory.OutputMemory, nodeEl);
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

        protected override void _OnCloned()
        {
            base._OnCloned();
            m_Tree = Variables.GetVariable("Tree");
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

        protected override void _OnCreateBase()
        {
            base._OnCreateBase();
            this.NodeMemory.vTypeChanged += _vTypeChanged;
        }
        /// <summary>
        /// Only change all the vtypes to the same when Output changes,
        /// because vtypes can be different.
        /// </summary>
        private void _vTypeChanged(Variable obj)
        {
            if (obj == Variables.GetVariable("Output"))
            {
                Variables.GetVariable("Input1").vType = obj.vType;
                Variables.GetVariable("Input2").vType = obj.vType;
            }
        }

        public override void CreateVariables()
        {
            Variable optr = NodeMemory.CreateVariable(
                "Operator",
                "+",
                Variable.CreateParams_Enum,
                Variable.CountType.CT_SINGLE,
                Variable.VariableType.VBT_Const,
                Variable.EnableType.ET_FIXED,
                0,
                0,
                "+|-|*|/"
            );

            Variable opl = NodeMemory.CreateVariable(
                "Output",
                "",
                Variable.CreateParams_CalculatorTypes,
                Variable.CountType.CT_SINGLE,
                Variable.VariableType.VBT_Pointer,
                Variable.EnableType.ET_FIXED
            );
            opl.IsInput = false;

            Variable opr1 = NodeMemory.CreateVariable(
                "Input1",
                "0",
                Variable.CreateParams_CalculatorTypes,
                Variable.CountType.CT_SINGLE,
                Variable.VariableType.VBT_NONE,
                Variable.EnableType.ET_FIXED
            );
            Variable opr2 = NodeMemory.CreateVariable(
                "Input2",
                "0",
                Variable.CreateParams_CalculatorTypes,
                Variable.CountType.CT_SINGLE,
                Variable.VariableType.VBT_NONE,
                Variable.EnableType.ET_FIXED
            );
        }

        public override string Note
        {
            get
            {
                return string.Format("{1} {2} {3} >> {0}",
                    Variables.GetVariable("Output").NoteValue,
                    Variables.GetVariable("Input1").NoteValue,
                    Variables.GetVariable("Operator").NoteValue,
                    Variables.GetVariable("Input2").NoteValue);
            }
        }

        protected override bool _OnCheckValid()
        {
            var tOutput = Variables.GetVariable("Output").vType;
            var tInput1 = Variables.GetVariable("Input1").vType;
            var tInput2 = Variables.GetVariable("Input2").vType;

            if ((tOutput == Variable.ValueType.VT_INT || tOutput == Variable.ValueType.VT_FLOAT)
                && tOutput == tInput1 && tOutput == tInput2)
                return true;
            var op = Variables.GetVariable("Operator").Value;
            if (tOutput == Variable.ValueType.VT_VECTOR3)
            {
                if (op == "+" || op == "-")
                {
                    if (tOutput == tInput1 && tOutput == tInput2)
                        return true;
                }
                else
                {
                    if (tInput1 == Variable.ValueType.VT_VECTOR3 && tInput2 == Variable.ValueType.VT_FLOAT)
                        return true;
                }
            }
            else if (tOutput == Variable.ValueType.VT_STRING)
            {
                if (op == "+")
                    return true;
            }

            LogMgr.Instance.Error(this.Renderer.UITitle + ": This type of calculate is not supported.");

            return false;
        }
    }

    class AlwaysSuccessTreeNode : SingleChildNode
    {
        public override string Icon => "T";

        public AlwaysSuccessTreeNode()
        {
            m_Name = "AlwaysSuccess";
            Type = TreeNodeType.TNT_Default;
        }
    }

    class AlwaysFailureTreeNode : SingleChildNode
    {
        public override string Icon => "F";

        public AlwaysFailureTreeNode()
        {
            m_Name = "AlwaysFailure";
            Type = TreeNodeType.TNT_Default;
        }
    }

    class ConvertToBoolTreeNode : SingleChildNode
    {
        public override string Icon => "B";

        public ConvertToBoolTreeNode()
        {
            m_Name = "ConvertToBool";
            Type = TreeNodeType.TNT_Default;
        }
        public override void CreateVariables()
        {
            var v = NodeMemory.CreateVariable(
                "Output",
                "",
                Variable.CreateParams_Bool,
                Variable.CountType.CT_SINGLE,
                Variable.VariableType.VBT_Pointer,
                Variable.EnableType.ET_FIXED
            );
            v.IsInput = false;
        }
    }
    //class InvertorTreeNode : SingleChildNode
    //{
    //    public override string Icon => "!";

    //    public InvertorTreeNode()
    //    {
    //        m_Name = "Invertor";
    //        Type = TreeNodeType.TNT_Default;
    //    }
    //}

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
                Variable.EnableType.ET_FIXED,
                0,
                0,
                "==|!=|>|<|>=|<="
            );

            Variable opl = NodeMemory.CreateVariable(
                "Opl",
                "",
                Variable.CreateParams_AllTypes,
                Variable.CountType.CT_SINGLE,
                Variable.VariableType.VBT_Pointer,
                Variable.EnableType.ET_FIXED,
                1
            );

            Variable opr = NodeMemory.CreateVariable(
                "Opr",
                "",
                Variable.CreateParams_AllTypes,
                Variable.CountType.CT_SINGLE,
                Variable.VariableType.VBT_NONE,
                Variable.EnableType.ET_FIXED,
                1
            );
        }

        public override string Note
        {
            get
            {
                return string.Format("{0} {1} {2} ?",
                    Variables.GetVariable("Opl").NoteValue,
                    Variables.GetVariable("Operator").Value,
                    Variables.GetVariable("Opr").NoteValue
                    );
            }
        }
    }

    class SetDataTreeNode : LeafNode
    {
        public override string Icon => "y >> x";

        public SetDataTreeNode()
        {
            m_Name = "SetData";
            Type = TreeNodeType.TNT_Default;
        }

        public override void CreateVariables()
        {
            Variable opl = NodeMemory.CreateVariable(
                "Target",
                "",
                Variable.CreateParams_AllTypes,
                Variable.CountType.CT_NONE,
                Variable.VariableType.VBT_Pointer,
                Variable.EnableType.ET_FIXED,
                1
            );
            //opl.IsInput = false;

            Variable opr = NodeMemory.CreateVariable(
                "Source",
                "",
                Variable.CreateParams_AllTypes,
                Variable.CountType.CT_NONE,
                Variable.VariableType.VBT_NONE,
                Variable.EnableType.ET_FIXED,
                1
            );
        }

        public override string Note
        {
            get
            {
                return string.Format("{1} >> {0}",
                    Variables.GetVariable("Target").NoteValue,
                    Variables.GetVariable("Source").NoteValue
                    );
            }
        }
    }
    class ConvertTreeNode : LeafNode
    {
        public override string Icon => "y >> x";

        Variable m_Target;
        Variable m_Source;
        public ConvertTreeNode()
        {
            m_Name = "Convert";
            Type = TreeNodeType.TNT_Default;
        }

        public override void CreateVariables()
        {
            m_Target = NodeMemory.CreateVariable(
                "Target",
                "",
                Variable.CreateParams_ConvertTypes,
                Variable.CountType.CT_SINGLE,
                Variable.VariableType.VBT_Pointer,
                Variable.EnableType.ET_FIXED
            );
            m_Target.IsInput = false;

            m_Source = NodeMemory.CreateVariable(
                "Source",
                "",
                Variable.CreateParams_ConvertTypes,
                Variable.CountType.CT_SINGLE,
                Variable.VariableType.VBT_NONE,
                Variable.EnableType.ET_FIXED
            );
        }
        protected override void _OnCloned()
        {
            base._OnCloned();
            m_Target = NodeMemory.GetVariable("Target");
            m_Source = NodeMemory.GetVariable("Source");
        }


        public override string Note
        {
            get
            {
                var targetType = Variable.ValueTypeDic2.GetValue(m_Target.vType, "");
                var sourceType = Variable.ValueTypeDic2.GetValue(m_Source.vType, "");
                return string.Format("({0}){1} >> ({2}){3}",
                    sourceType,
                    m_Source.NoteValue,
                    targetType,
                    m_Target.NoteValue
                    );
            }
        }

        static HashSet<ConvertType> s_SupportedTypes = new HashSet<ConvertType>
    {
        new ConvertType(Variable.ValueType.VT_INT, Variable.ValueType.VT_STRING),
        new ConvertType(Variable.ValueType.VT_FLOAT, Variable.ValueType.VT_STRING),
        new ConvertType(Variable.ValueType.VT_BOOL, Variable.ValueType.VT_STRING),
        new ConvertType(Variable.ValueType.VT_INT, Variable.ValueType.VT_BOOL),
        new ConvertType(Variable.ValueType.VT_INT, Variable.ValueType.VT_FLOAT),
    };
        protected override bool _OnCheckValid()
        {
            ConvertType type = new ConvertType(m_Source.vType, m_Target.vType);
            if (s_SupportedTypes.Contains(type))
                return true;

            type = new ConvertType(m_Target.vType, m_Source.vType);
            if (s_SupportedTypes.Contains(type))
                return true;

            LogMgr.Instance.Error(this.Renderer.UITitle + ": This type of convert is not supported.");

            return false;
        }
    }


    class IfThenElseTreeNode : TreeNode
    {
        public override string Icon => "↙ ？↘";

        public IfThenElseTreeNode()
        {
            m_Name = "IfThenElse";
            Type = TreeNodeType.TNT_Default;

            m_Connections.Add("if", false, Connector.PosType.CHILDREN);
            m_Connections.Add("then", false, Connector.PosType.CHILDREN);
            m_Connections.Add("else", false, Connector.PosType.CHILDREN);
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
                "",
                Variable.CreateParams_RandomTypes,
                Variable.CountType.CT_SINGLE,
                Variable.VariableType.VBT_Pointer,
                Variable.EnableType.ET_FIXED,
                1
            );
            opl.IsInput = false;

            Variable opr1 = NodeMemory.CreateVariable(
                "Bound1",
                "",
                Variable.CreateParams_RandomTypes,
                Variable.CountType.CT_SINGLE,
                Variable.VariableType.VBT_NONE,
                Variable.EnableType.ET_FIXED,
                1
            );

            Variable opr2 = NodeMemory.CreateVariable(
                "Bound2",
                "",
                Variable.CreateParams_RandomTypes,
                Variable.CountType.CT_SINGLE,
                Variable.VariableType.VBT_NONE,
                Variable.EnableType.ET_FIXED,
                1
            );
        }

        public override string Note
        {
            get
            {
                return string.Format("[ {1} ~ {2} ) >> {0}",
                    Variables.GetVariable("Target").NoteValue,
                    Variables.GetVariable("Bound1").NoteValue,
                    Variables.GetVariable("Bound2").NoteValue);
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
            Variable opr = NodeMemory.CreateVariable(
                "Input",
                "",
                Variable.CreateParams_AllTypes,
                Variable.CountType.CT_LIST,
                Variable.VariableType.VBT_NONE,
                Variable.EnableType.ET_FIXED,
                1
            );

            Variable opl = NodeMemory.CreateVariable(
                "Output",
                "",
                Variable.CreateParams_AllTypes,
                Variable.CountType.CT_SINGLE,
                Variable.VariableType.VBT_Pointer,
                Variable.EnableType.ET_FIXED,
                1
            );
            opl.IsInput = false;
        }

        public override string Note
        {
            get
            {
                return string.Format("{0} ~=> {1}",
                    Variables.GetVariable("Input").NoteValue,
                    Variables.GetVariable("Output").NoteValue);
            }
        }
    }

    class HandleEventTreeNode : CompositeNode
    {
        public override string Icon => "[↙↓↘]";
        Variable m_Events;

        public HandleEventTreeNode()
        {
            m_Name = "HandleEvent";
            Type = TreeNodeType.TNT_Default;
        }

        public override void CreateVariables()
        {
            NodeMemory.CreateVariable(
                "Type",
                "Latest",
                Variable.CreateParams_Enum,
                Variable.CountType.CT_SINGLE,
                Variable.VariableType.VBT_Const,
                Variable.EnableType.ET_FIXED,
                0,
                0,
                "Latest|Every"
            );

            m_Events = NodeMemory.CreateVariable(
                "Events",
                "",
                Variable.CreateParams_String,
                Variable.CountType.CT_LIST,
                Variable.VariableType.VBT_NONE,
                Variable.EnableType.ET_Enable
            );
            m_Events.IsInput = true;

            Variable ints = NodeMemory.CreateVariable(
                "Int",
                "",
                Variable.CreateParams_Int,
                Variable.CountType.CT_LIST,
                Variable.VariableType.VBT_Pointer,
                Variable.EnableType.ET_Disable
            );
            ints.IsInput = false;

            Variable floats = NodeMemory.CreateVariable(
                "Float",
                "",
                Variable.CreateParams_Float,
                Variable.CountType.CT_LIST,
                Variable.VariableType.VBT_Pointer,
                Variable.EnableType.ET_Disable
            );
            floats.IsInput = false;

            Variable strings = NodeMemory.CreateVariable(
                "String",
                "",
                Variable.CreateParams_String,
                Variable.CountType.CT_LIST,
                Variable.VariableType.VBT_Pointer,
                Variable.EnableType.ET_Disable
            );
            strings.IsInput = false;

            Variable ulongs = NodeMemory.CreateVariable(
                "Ulong",
                "",
                Variable.CreateParams_Ulong,
                Variable.CountType.CT_LIST,
                Variable.VariableType.VBT_Pointer,
                Variable.EnableType.ET_Disable
            );
            ulongs.IsInput = false;

            Variable bools = NodeMemory.CreateVariable(
                "Bool",
                "",
                Variable.CreateParams_Bool,
                Variable.CountType.CT_LIST,
                Variable.VariableType.VBT_Pointer,
                Variable.EnableType.ET_Disable
            );
            bools.IsInput = false;

            Variable vector3 = NodeMemory.CreateVariable(
                "Vector3",
                "",
                Variable.CreateParams_Vector3,
                Variable.CountType.CT_LIST,
                Variable.VariableType.VBT_Pointer,
                Variable.EnableType.ET_Disable
            );
            vector3.IsInput = false;

            Variable entity = NodeMemory.CreateVariable(
                "Entity",
                "",
                Variable.CreateParams_Entity,
                Variable.CountType.CT_LIST,
                Variable.VariableType.VBT_Pointer,
                Variable.EnableType.ET_Disable
            );
            entity.IsInput = false;

            Variable current = NodeMemory.CreateVariable(
                "Current",
                "",
                Variable.CreateParams_String,
                Variable.CountType.CT_SINGLE,
                Variable.VariableType.VBT_Pointer,
                Variable.EnableType.ET_Disable
            );
            current.IsInput = false;
        }

        public override string Note
        {
            get
            {
                var v = Variables.GetVariable("Events");
                if (v.eType == Variable.EnableType.ET_Disable)
                    return "All Events";
                else
                    return v.NoteValue;
            }
        }

        protected override void _OnCloned()
        {
            base._OnCloned();
            m_Events = NodeMemory.GetVariable("Events");
        }

        protected override bool _OnCheckValid()
        {
            if (m_Events.eType != Variable.EnableType.ET_Disable && m_Events.vbType == Variable.VariableType.VBT_Const)
            {
                if (_GetCasesValue() == null)
                {
                    LogMgr.Instance.Error(this.Renderer.UITitle + ": invalid Events");
                    return false;
                }
            }
            else
            {
                if (this.m_Connections.GetConnector(Connector.IdentifierChildren, Connector.PosType.CHILDREN).Conns.Count != 1)
                {
                    LogMgr.Instance.Error(this.Renderer.UITitle + ": children count must be one");
                    return false;
                }
            }
            return true;
        }

        protected override void _OnVariableValueChanged(Variable v)
        {
            if (v == m_Events)
            {
                _RefreshChildrenNotes();
            }
        }

        public override void OnConnectToChanged()
        {
            base.OnConnectToChanged();
            _RefreshChildrenNotes();
        }

        string[] _GetCasesValue()
        {
            if (m_Events.eType == Variable.EnableType.ET_Disable)
                return null;
            if (m_Events.vbType == Variable.VariableType.VBT_Const)
            {
                if (string.IsNullOrEmpty(m_Events.Value))
                {
                    if (Tree != null && !Tree.IsInState(Graph.FLAG_LOADING))
                        LogMgr.Instance.Error("Events cant be empty " + (this.Renderer == null ? this.NickName : this.Renderer.UITitle));
                    return null;
                }
                var childCount = this.m_Connections.GetConnector(Connector.IdentifierChildren, Connector.PosType.CHILDREN).Conns.Count;
                if (childCount == 1)
                    return new string[] { m_Events.Value };
                string[] ss = m_Events.Value.Split(Variable.ListSpliter);
                if (ss.Length != childCount)
                {
                    if (Tree != null && !Tree.IsInState(Graph.FLAG_LOADING))
                        LogMgr.Instance.Error("Events size not match in " + (this.Renderer == null ? this.NickName : this.Renderer.UITitle));
                    return null;
                }
                return ss;
            }
            return null;
        }

        protected void _RefreshChildrenNotes()
        {
            var conns = m_Connections.GetConnector(Connector.IdentifierChildren, Connector.PosType.CHILDREN).Conns;
            var values = _GetCasesValue();
            if (values != null)
            {
                for (int i = 0; i < conns.Count; ++i)
                {
                    conns[i].Note = values[i];
                }
            }
            else
            {
                if (conns.Count == 1)
                    conns[0].Note = string.Empty;
                else
                {
                    for (int i = 0; i < conns.Count; ++i)
                    {
                        conns[i].Note = "No." + i;
                    }
                }
            }
        }

    }

    class SwitchCaseTreeNode : CompositeNode
    {
        public override string Icon => "↙↓↘";
        Variable m_Cases;
        public SwitchCaseTreeNode()
        {
            m_Name = "SwitchCase";
            Type = TreeNodeType.TNT_Default;

            m_Connections.Add(Connector.IdentifierDefault, false, Connector.PosType.CHILDREN);
        }

        public override void CreateVariables()
        {
            NodeMemory.CreateVariable(
                "Switch",
                "",
                Variable.CreateParams_SwitchTypes,
                Variable.CountType.CT_SINGLE,
                Variable.VariableType.VBT_Pointer,
                Variable.EnableType.ET_FIXED,
                1
            );

            m_Cases = 
            NodeMemory.CreateVariable(
                "Cases",
                "",
                Variable.CreateParams_SwitchTypes,
                Variable.CountType.CT_LIST,
                Variable.VariableType.VBT_NONE,
                Variable.EnableType.ET_FIXED,
                1
            );
        }

        protected override void _OnCloned()
        {
            base._OnCloned();
            m_Cases = NodeMemory.GetVariable("Cases");
        }

        public override string Note
        {
            get
            {
                return string.Format("{0} from {{ {1} }}",
                    Variables.GetVariable("Switch").NoteValue,
                    Variables.GetVariable("Cases").NoteValue);
            }
        }

        protected override bool _OnCheckValid()
        {
            if (m_Cases.vbType == Variable.VariableType.VBT_Const)
            {
                return _GetCasesValue() != null;
            }
            return true;
        }

        protected override void _OnVariableValueChanged(Variable v)
        {
            if (v == m_Cases)
            {
                _RefreshChildrenNotes();
            }
        }

        public override void OnConnectToChanged()
        {
            base.OnConnectToChanged();
            _RefreshChildrenNotes();
        }

        string[] _GetCasesValue()
        {
            if (m_Cases.vbType == Variable.VariableType.VBT_Const)
            {
                if (string.IsNullOrEmpty(m_Cases.Value))
                {
                    if (Tree != null && !Tree.IsInState(Graph.FLAG_LOADING))
                        LogMgr.Instance.Error("Cases cant be empty " + (this.Renderer == null ? this.NickName : this.Renderer.UITitle));
                    return null;
                }

                string[] ss = m_Cases.Value.Split(Variable.ListSpliter);
                if (ss.Length != this.m_Connections.GetConnector(Connector.IdentifierChildren, Connector.PosType.CHILDREN).Conns.Count)
                {
                    if (Tree != null && !Tree.IsInState(Graph.FLAG_LOADING))
                        LogMgr.Instance.Error("Cases size not match in " + (this.Renderer == null ? this.NickName : this.Renderer.UITitle));
                    return null;
                }
                return ss;
            }
            return null;
        }

        protected void _RefreshChildrenNotes()
        {
            var conns = m_Connections.GetConnector(Connector.IdentifierChildren, Connector.PosType.CHILDREN).Conns;
            var values = _GetCasesValue();
            if (values != null)
            {
                for (int i = 0; i < conns.Count; ++i)
                {
                    conns[i].Note = values[i];
                }
            }
            else
            {
                if (conns.Count == 1)
                    conns[0].Note = string.Empty;
                else
                {
                    for (int i = 0; i < conns.Count; ++i)
                    {
                        conns[i].Note = "No." + i;
                    }
                }
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

            m_Connections.Add(Connector.IdentifierInit, false, Connector.PosType.CHILDREN);
            m_Connections.Add(Connector.IdentifierCond, false, Connector.PosType.CHILDREN);
            m_Connections.Add(Connector.IdentifierIncrement, false, Connector.PosType.CHILDREN);

            ///> to make the "chilren" conn the last conn
            m_Ctr = m_Connections.Add(Connector.IdentifierChildren, false, Connector.PosType.CHILDREN);
        }

        public override void CreateVariables()
        {
            NodeMemory.CreateVariable(
                "BreakValue",
                "F",
                Variable.CreateParams_Bool,
                Variable.CountType.CT_SINGLE,
                Variable.VariableType.VBT_NONE,
                Variable.EnableType.ET_Disable
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
                Variable.EnableType.ET_FIXED,
                1
            );
            var v = NodeMemory.CreateVariable(
                "Current",
                "",
                Variable.CreateParams_AllTypes,
                Variable.CountType.CT_SINGLE,
                Variable.VariableType.VBT_Pointer,
                Variable.EnableType.ET_FIXED,
                1
            );
            v.IsInput = false;

            NodeMemory.CreateVariable(
                "BreakValue",
                "F",
                Variable.CreateParams_Bool,
                Variable.CountType.CT_SINGLE,
                Variable.VariableType.VBT_NONE,
                Variable.EnableType.ET_Disable
            );
        }

        public override string Note
        {
            get
            {
                return string.Format("{{ {1} }} >> {0}",
                    Variables.GetVariable("Current").NoteValue,
                    Variables.GetVariable("Collection").NoteValue);
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
                Variable.EnableType.ET_FIXED,
                1
            );
            var v = NodeMemory.CreateVariable(
                "Current",
                "",
                Variable.CreateParams_Int,
                Variable.CountType.CT_SINGLE,
                Variable.VariableType.VBT_Pointer,
                Variable.EnableType.ET_FIXED,
                1
            );
            v.IsInput = false;

            NodeMemory.CreateVariable(
                "BreakValue",
                "F",
                Variable.CreateParams_Bool,
                Variable.CountType.CT_SINGLE,
                Variable.VariableType.VBT_NONE,
                Variable.EnableType.ET_Disable
            );
        }

        public override string Note
        {
            get
            {
                return string.Format("[0, {1}) >> {0}",
                    Variables.GetVariable("Current").NoteValue,
                    Variables.GetVariable("Count").NoteValue);
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
                Variable.EnableType.ET_FIXED,
                1
            );

            NodeMemory.CreateVariable(
                "KeyPointY",
                "",
                Variable.CreateParams_AllNumbers,
                Variable.CountType.CT_LIST,
                Variable.VariableType.VBT_NONE,
                Variable.EnableType.ET_FIXED,
                1
            );

            NodeMemory.CreateVariable(
                "InputX",
                "",
                Variable.CreateParams_AllNumbers,
                Variable.CountType.CT_SINGLE,
                Variable.VariableType.VBT_NONE,
                Variable.EnableType.ET_FIXED,
                1
            );

            var v = NodeMemory.CreateVariable(
                "OutputY",
                "",
                Variable.CreateParams_AllNumbers,
                Variable.CountType.CT_SINGLE,
                Variable.VariableType.VBT_Pointer,
                Variable.EnableType.ET_FIXED,
                1
            );
            v.IsInput = false;

        }

        public override string Note
        {
            get
            {
                return string.Format("func({1}) >> {0}",
                    Variables.GetVariable("OutputY").NoteValue,
                    Variables.GetVariable("InputX").NoteValue);
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
                Variable.EnableType.ET_FIXED,
                1
            );

            NodeMemory.CreateVariable(
                "Values",
                "",
                Variable.CreateParams_AllTypes,
                Variable.CountType.CT_LIST,
                Variable.VariableType.VBT_NONE,
                Variable.EnableType.ET_FIXED,
                2
            );

            NodeMemory.CreateVariable(
                "Input",
                "0",
                Variable.CreateParams_AllNumbers,
                Variable.CountType.CT_SINGLE,
                Variable.VariableType.VBT_NONE,
                Variable.EnableType.ET_Disable,
                1
            );

            var v = NodeMemory.CreateVariable(
                "Output",
                "",
                Variable.CreateParams_AllTypes,
                Variable.CountType.CT_SINGLE,
                Variable.VariableType.VBT_Pointer,
                Variable.EnableType.ET_FIXED,
                2
            );
            v.IsInput = false;

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
                Variable.VariableType.VBT_NONE,
                Variable.EnableType.ET_FIXED
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
                Variable.VariableType.VBT_Pointer,
                Variable.EnableType.ET_FIXED
            );
            //opl.IsInput = false;

        }

        public override string Note
        {
            get
            {
                return string.Format("{0}",
                    Variables.GetVariable("Array").NoteValue
                    );
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
                Variable.VariableType.VBT_Pointer,
                Variable.EnableType.ET_FIXED
            );
        }

        public override string Note
        {
            get
            {
                return string.Format("{0}",
                    Variables.GetVariable("Array").NoteValue
                    );
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
                Variable.VariableType.VBT_NONE,
                Variable.EnableType.ET_FIXED
            );

            Variable length = NodeMemory.CreateVariable(
                "Length",
                "",
                Variable.CreateParams_Int,
                Variable.CountType.CT_SINGLE,
                Variable.VariableType.VBT_Pointer,
                Variable.EnableType.ET_FIXED
            );
            length.IsInput = false;

        }

        public override string Note
        {
            get
            {
                return string.Format("[{1}].Length >> {0}",
                    Variables.GetVariable("Length").NoteValue,
                    Variables.GetVariable("Array").NoteValue
                    );
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
                Variable.EnableType.ET_FIXED,
                1
            );
            //array.IsInput = false;

            Variable element = NodeMemory.CreateVariable(
                "Element",
                "",
                Variable.CreateParams_AllTypes,
                Variable.CountType.CT_SINGLE,
                Variable.VariableType.VBT_NONE,
                Variable.EnableType.ET_FIXED,
                1
            );
        }

        public override string Note
        {
            get
            {
                return string.Format("[{0}] <= {1}",
                    Variables.GetVariable("Array").NoteValue,
                    Variables.GetVariable("Element").NoteValue
                    );
            }
        }
    }
    class ArrayRemoveElementTreeNode : LeafNode
    {
        public override string Icon => "[x] rm y";

        public ArrayRemoveElementTreeNode()
        {
            m_Name = "ArrayRemoveElement";
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
                Variable.EnableType.ET_FIXED,
                1
            );
            //array.IsInput = false;

            Variable element = NodeMemory.CreateVariable(
                "Element",
                "",
                Variable.CreateParams_AllTypes,
                Variable.CountType.CT_SINGLE,
                Variable.VariableType.VBT_NONE,
                Variable.EnableType.ET_FIXED,
                1
            );

            Variable isAll = NodeMemory.CreateVariable(
                "IsAll",
                "F",
                Variable.CreateParams_Bool,
                Variable.CountType.CT_SINGLE,
                Variable.VariableType.VBT_NONE,
                Variable.EnableType.ET_FIXED
            );
        }

        public override string Note
        {
            get
            {
                return string.Format("[{0}] remove {1}",
                    Variables.GetVariable("Array").NoteValue,
                    Variables.GetVariable("Element").NoteValue
                    );
            }
        }
    }

    class ArrayHasElementTreeNode : LeafNode
    {
        public override string Icon => "[x] has y?";

        public ArrayHasElementTreeNode()
        {
            m_Name = "ArrayHasElement";
            Type = TreeNodeType.TNT_Default;
        }

        public override void CreateVariables()
        {
            Variable array = NodeMemory.CreateVariable(
                "Array",
                "",
                Variable.CreateParams_AllTypes,
                Variable.CountType.CT_LIST,
                Variable.VariableType.VBT_NONE,
                Variable.EnableType.ET_FIXED,
                1
            );
            //array.IsInput = false;

            Variable element = NodeMemory.CreateVariable(
                "Element",
                "",
                Variable.CreateParams_AllTypes,
                Variable.CountType.CT_SINGLE,
                Variable.VariableType.VBT_NONE,
                Variable.EnableType.ET_FIXED,
                1
            );

            Variable count = NodeMemory.CreateVariable(
                "Count",
                "",
                Variable.CreateParams_Int,
                Variable.CountType.CT_SINGLE,
                Variable.VariableType.VBT_Pointer,
                Variable.EnableType.ET_Disable
            );
            count.IsInput = false;
            Variable index = NodeMemory.CreateVariable(
                "Index",
                "",
                Variable.CreateParams_Int,
                Variable.CountType.CT_SINGLE,
                Variable.VariableType.VBT_Pointer,
                Variable.EnableType.ET_Disable
            );
            index.IsInput = false;
        }

        public override string Note
        {
            get
            {
                return string.Format("[{0}] has {1}?",
                    Variables.GetVariable("Array").NoteValue,
                    Variables.GetVariable("Element").NoteValue
                    );
            }
        }
    }


    class GenIndexArrayTreeNode : LeafNode
    {
        public override string Icon => "[012..]";

        public GenIndexArrayTreeNode()
        {
            m_Name = "GenIndexArray";
            Type = TreeNodeType.TNT_Default;
        }

        public override void CreateVariables()
        {
            Variable input = NodeMemory.CreateVariable(
                "Input",
                "",
                Variable.CreateParams_AllTypes,
                Variable.CountType.CT_NONE,
                Variable.VariableType.VBT_NONE,
                Variable.EnableType.ET_FIXED
            );

            Variable output = NodeMemory.CreateVariable(
                "Output",
                "",
                Variable.CreateParams_Int,
                Variable.CountType.CT_LIST,
                Variable.VariableType.VBT_Pointer,
                Variable.EnableType.ET_FIXED
            );
            output.IsInput = false;

        }

        public override string Note
        {
            get
            {
                var v = Variables.GetVariable("Input");
                if (v.cType == Variable.CountType.CT_LIST)
                    return string.Format("{0} will have same length with {1}",
                        Variables.GetVariable("Output").NoteValue,
                        v.NoteValue
                        );
                else
                    return string.Format("{0} will have length of {1}",
                        Variables.GetVariable("Output").NoteValue,
                        v.NoteValue
                        );
            }
        }

        protected override bool _OnCheckValid()
        {
            var v = Variables.GetVariable("Input");
            if (v.cType == Variable.CountType.CT_SINGLE && v.vType != Variable.ValueType.VT_INT)
            {
                LogMgr.Instance.Error("Input can only be INT when it's not an array");
                return false;
            }
            return true;
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
                Variable.EnableType.ET_FIXED,
                1
            );

            Variable output = NodeMemory.CreateVariable(
                "Output",
                "",
                Variable.CreateParams_AllTypes,
                Variable.CountType.CT_LIST,
                Variable.VariableType.VBT_Pointer,
                Variable.EnableType.ET_FIXED,
                1
            );
            output.IsInput = false;

        }

        public override string Note
        {
            get
            {
                return string.Format("[{1}] >??> [{0}]",
                    Variables.GetVariable("Output").NoteValue,
                    Variables.GetVariable("Input").NoteValue
                    );
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
                Variable.EnableType.ET_FIXED,
                1
            );
            Variable optr = NodeMemory.CreateVariable(
                "Operator",
                "On",
                Variable.CreateParams_Enum,
                Variable.CountType.CT_SINGLE,
                Variable.VariableType.VBT_Const,
                Variable.EnableType.ET_FIXED,
                0,
                0,
                "On|Off"
            );
        }

        public override string Note
        {
            get
            {
                return string.Format("Set Conditions {0}: {1}",
                    Variables.GetVariable("Operator").NoteValue,
                    Variables.GetVariable("Conditions").NoteValue
                    );
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
    }

    class SetOperationTreeNode : LeafNode
    {
        public override string Icon => "[x]+-*/[y]";

        public SetOperationTreeNode()
        {
            m_Name = "SetOperation";
            Type = TreeNodeType.TNT_Default;
        }

        public override void CreateVariables()
        {
            Variable optr = NodeMemory.CreateVariable(
                "Operator",
                "APPEND",
                Variable.CreateParams_Enum,
                Variable.CountType.CT_SINGLE,
                Variable.VariableType.VBT_Const,
                Variable.EnableType.ET_FIXED,
                0,
                0,
                "APPEND|MERGE"
            );

            Variable opl = NodeMemory.CreateVariable(
                "Output",
                "",
                Variable.CreateParams_AllTypes,
                Variable.CountType.CT_LIST,
                Variable.VariableType.VBT_Pointer,
                Variable.EnableType.ET_FIXED,
                1
            );
            opl.IsInput = false;

            Variable opr1 = NodeMemory.CreateVariable(
                "Input1",
                "",
                Variable.CreateParams_AllTypes,
                Variable.CountType.CT_LIST,
                Variable.VariableType.VBT_NONE,
                Variable.EnableType.ET_FIXED,
                1
            );
            Variable opr2 = NodeMemory.CreateVariable(
                "Input2",
                "",
                Variable.CreateParams_AllTypes,
                Variable.CountType.CT_LIST,
                Variable.VariableType.VBT_NONE,
                Variable.EnableType.ET_FIXED,
                1
            );
        }

        public override string Note
        {
            get
            {
                return string.Format("{1} {2} {3} >> {0}",
                    Variables.GetVariable("Output").NoteValue,
                    Variables.GetVariable("Input1").NoteValue,
                    Variables.GetVariable("Operator").NoteValue,
                    Variables.GetVariable("Input2").NoteValue);
            }
        }
    }

}
