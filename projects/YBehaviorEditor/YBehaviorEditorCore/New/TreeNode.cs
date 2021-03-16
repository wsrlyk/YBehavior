using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace YBehavior.Editor.Core.New
{
    public class TreeNodeWrapper : NodeWrapper, IVariableCollectionOwner
    {
        public Tree Tree { get { return Graph as Tree; } }
        public IVariableDataSource VariableDataSource { get { return Tree; } }
        public TreeMemory SharedData { get { return Tree == null ? null : Tree.SharedData; } }
        public InOutMemory InOutData { get { return Tree == null ? null : Tree.InOutData; } }

        public void OnVariableValueChanged(Variable v)
        {
            (Node as TreeNode).OnVariableValueChanged(v);
        }
    }

    public enum TreeNodeType
    {
        TNT_Invalid,
        TNT_Root,
        TNT_Default,
        TNT_External,
    }

    public class TreeNodeMgr : Singleton<TreeNodeMgr>
    {
        List<TreeNode> m_NodeList = new List<TreeNode>();
        public List<TreeNode> NodeList { get { return m_NodeList; } }
        private Dictionary<string, Type> m_TypeDic = new Dictionary<string, Type>();

        public TreeNodeMgr()
        {
            var subTypeQuery = from t in System.Reflection.Assembly.GetExecutingAssembly().GetTypes()
                               where Utility.IsSubClassOf(t, typeof(TreeNode))
                               select t;

            foreach (var type in subTypeQuery)
            {
                TreeNode node = Activator.CreateInstance(type) as TreeNode;
                if (node.Type == TreeNodeType.TNT_Invalid)
                    continue;
                node.CreateBase();
                node.CreateVariables();
                node.LoadDescription();
                m_NodeList.Add(node);
                m_TypeDic.Add(node.Name, type);
                Console.WriteLine(type);
            }
        }

        public TreeNode CreateNodeByName(string name)
        {
            TreeNode node = null;
            if (m_TypeDic.TryGetValue(name, out Type type))
            {
                node = Activator.CreateInstance(type) as TreeNode;
                node.CreateBase();
                node.CreateVariables();
                node.LoadDescription();
                return node;
            }

            node = ExternalActionMgr.Instance.GetTreeNode(name);
            if (node != null)
            {
            }
            return node;
        }
    }

    public class TreeNode : NodeBase
    {
        private string m_ReturnType = "Default";
        protected bool m_Folded = false;
        protected int m_SelfDisabled = 0;
        public override bool SelfDisabled { get { return m_SelfDisabled > 0; } }
        public TreeNodeType Type { get; set; }
        public int Hierachy { get; set; }
        public bool IsChildrenRendering { get { return !m_Folded; } }
        public bool Folded { get { return m_Folded; } set { m_Folded = value; } }
        public string ReturnType { get { return m_ReturnType; } set { m_ReturnType = value; } }

        protected NodeMemory m_NodeMemory;
        public NodeMemory NodeMemory { get { return m_NodeMemory; } }
        protected IVariableCollection m_Variables;
        public IVariableCollection Variables { get { return m_Variables; } }

        TypeMap m_TypeMap = new TypeMap();
        public TypeMap TypeMap { get { return m_TypeMap; } }

        public Tree Tree { get { return Graph as Tree; } }

        public override IEnumerable<string> TextForFilter { get { return new TextForFilterGetter<TreeNodeTextForFilter>(this); } }

        public static readonly HashSet<string> ReservedAttributes = new HashSet<string>(new string[] { "Class", "Connection" });
        public static readonly HashSet<string> ReservedAttributesAll = new HashSet<string>(new string[] { "Class", "Pos", "NickName", "Connection", "DebugPoint", "Comment" });

        public TreeNode()
        {
        }

        public override NodeBase Clone()
        {
            TreeNode other = base.Clone() as TreeNode;
            other.Type = Type;
            other.Hierachy = Hierachy;
            if (Variables is NodeMemory)
            {
                (other.Variables as NodeMemory).CloneFrom(Variables as NodeMemory);
            }
            other.TypeMap.CloneFrom(TypeMap);

            other._OnCloned();
            return other;
        }

        virtual protected void _OnCloned() { }
        protected override void _CreateWrapper()
        {
            m_Wrapper = new TreeNodeWrapper();
        }

        public override void CreateBase()
        {
            base.CreateBase();
            if (Type != TreeNodeType.TNT_Root)
                Conns.Add(Connector.IdentifierParent, false);

            if (Type == TreeNodeType.TNT_Root)
            {
                m_Variables = new TreeMemory(m_Wrapper as TreeNodeWrapper);
            }
            else
            {
                m_Variables = new NodeMemory(m_Wrapper as TreeNodeWrapper);
                m_NodeMemory = m_Variables as NodeMemory;
            }
            m_ConditonConnector = Conns.Add(Connector.IdentifierCondition, false);

            _OnCreateBase();
        }

        protected virtual void _OnCreateBase()
        {

        }
        protected override void _CreateRenderer()
        {
            m_Renderer = new TreeNodeRenderer(this);
        }

        public TreeNode Parent
        {
            get
            {
                if (m_Connections.ParentConnector == null || m_Connections.ParentConnector.Conns.Count == 0)
                    return null;
                return m_Connections.ParentConnector.Conns[0].Ctr.From.Owner as TreeNode;
            }
        }

        public TreeNode Root
        {
            get
            {
                TreeNode root = this;
                TreeNode parent = this;
                while (parent != null)
                {
                    root = parent;
                    parent = parent.Parent;
                }
                return root;
            }
        }

        public virtual void CreateVariables()
        {

        }

        public void LoadDescription()
        {
            m_NodeDescripion = DescriptionMgr.Instance.GetNodeDescription(this.Name);
            if (m_NodeDescripion != null && m_Variables != null)
            {
                foreach (var v in m_Variables.Datas)
                {
                    v.Variable.Description = m_NodeDescripion == null ? string.Empty : m_NodeDescripion.GetVariable(v.Variable.Name);
                }
            }
        }

        public void Load(System.Xml.XmlNode data)
        {
            foreach (System.Xml.XmlAttribute attr in data.Attributes)
            {
                if (ReservedAttributes.Contains(attr.Name))
                    continue;
                ProcessAttrWhenLoad(attr);
            }

            _OnLoaded();
        }

        protected virtual void _OnLoaded()
        {

        }

        public void LoadChild(System.Xml.XmlNode data)
        {
            _OnLoadChild(data);
        }

        protected virtual void _OnLoadChild(System.Xml.XmlNode data)
        {

        }
        protected bool ProcessAttrWhenLoad(System.Xml.XmlAttribute attr)
        {
            switch (attr.Name)
            {
                case "Pos":
                    Geo.Pos = Point.Parse(attr.Value);
                    break;
                case "NickName":
                    m_NickName = attr.Value;
                    break;
                case "Comment":
                    Comment = attr.Value;
                    break;
                case "Disabled":
                    Disabled = bool.Parse(attr.Value);
                    break;
                case "Return":
                    m_ReturnType = attr.Value;
                    break;
                default:
                    return LoadOtherAttr(attr);
            }

            return true;
        }

        protected virtual bool LoadOtherAttr(System.Xml.XmlAttribute attr)
        {
            return DefaultLoadVariable(attr);
        }
        protected bool DefaultLoadVariable(System.Xml.XmlAttribute attr)
        {
            Variable v = Variables.GetVariable(attr.Name);
            if (v != null)
            {
                if (!v.SetVariableInNode(attr.Value))
                    return false;
                if (v.CheckValid())
                {
                    return true;
                }
            }
            return false;
        }

        public void Save(System.Xml.XmlElement data, System.Xml.XmlDocument xmlDoc)
        {
            if (Conns.ParentConnector != null && Conns.ParentConnector.Conns.Count > 0)
            {
                if (m_Connections.ParentConnector.Conns[0].Ctr.From.Identifier != Connector.IdentifierChildren)
                    data.SetAttribute("Connection", m_Connections.ParentConnector.Conns[0].Ctr.From.Identifier);
            }

            if (ReturnType != "Default")
                data.SetAttribute("Return", ReturnType);

            Point intPos = new Point((int)Geo.Pos.X, (int)Geo.Pos.Y);
            data.SetAttribute("Pos", intPos.ToString());
            if (!string.IsNullOrEmpty(m_NickName))
                data.SetAttribute("NickName", m_NickName);

            //if (!DebugPointInfo.NoDebugPoint)
            //    data.SetAttribute("DebugPoint", DebugPointInfo.HitCount.ToString());

            if (!string.IsNullOrEmpty(Comment))
            {
                data.SetAttribute("Comment", Comment);
            }

            if (SelfDisabled)
            {
                data.SetAttribute("Disabled", "true");
            }

            _OnSaveVariables(data, xmlDoc);
        }

        protected void _SaveVariables(IVariableCollection collection, System.Xml.XmlElement data)
        {
            foreach (VariableHolder v in collection.Datas)
            {
                data.SetAttribute(v.Variable.Name, v.Variable.ValueInXml);
            }
        }
        protected void _ExportVariables(IVariableCollection collection, System.Xml.XmlElement data)
        {
            foreach (VariableHolder v in collection.Datas)
            {
                if (v.Variable.eType == Variable.EnableType.ET_Disable)
                    continue;
                data.SetAttribute(v.Variable.Name, v.Variable.ValueInXml);
            }
        }
        protected virtual void _OnSaveVariables(System.Xml.XmlElement data, System.Xml.XmlDocument xmlDoc)
        {
            _SaveVariables(Variables, data);
        }
        public void Export(System.Xml.XmlElement data, System.Xml.XmlDocument xmlDoc)
        {
            if (Conns.ParentConnector != null && Conns.ParentConnector.Conns.Count > 0)
            {
                if (m_Connections.ParentConnector.Conns[0].Ctr.From.Identifier != Connector.IdentifierChildren)
                    data.SetAttribute("Connection", m_Connections.ParentConnector.Conns[0].Ctr.From.Identifier);
            }

            if (ReturnType != "Default")
                data.SetAttribute("Return", ReturnType);

            _OnExportVariables(data, xmlDoc);
        }
        protected virtual void _OnExportVariables(System.Xml.XmlElement data, System.Xml.XmlDocument xmlDoc)
        {
            _ExportVariables(Variables, data);
        }

        public override bool CheckValid()
        {
            bool bRes = true;
            if (m_Variables is NodeMemory)
            {
                NodeMemory memory = m_Variables as NodeMemory;
                SameTypeGroup vTypeGroup = memory.vTypeGroup;
                if (vTypeGroup != null)
                {
                    foreach (HashSet<string> group in vTypeGroup)
                    {
                        Variable.ValueType valueType = Variable.ValueType.VT_NONE;
                        foreach (string vName in group)
                        {
                            Variable v = memory.GetVariable(vName);
                            if (v == null)
                                continue;

                            if (valueType == Variable.ValueType.VT_NONE)
                                valueType = v.vType;
                            else if (valueType != v.vType)
                            {
                                LogMgr.Instance.Log("ValueType not match in Node: " + Renderer.UITitle + "." + vName);
                                bRes = false;
                            }
                        }
                    }
                }
                SameTypeGroup cTypeGroup = memory.cTypeGroup;
                if (cTypeGroup != null)
                {
                    foreach (HashSet<string> group in cTypeGroup)
                    {
                        Variable.CountType countType = Variable.CountType.CT_NONE;
                        foreach (string vName in group)
                        {
                            Variable v = memory.GetVariable(vName);
                            if (v == null)
                                continue;

                            if (countType == Variable.CountType.CT_NONE)
                                countType = v.cType;
                            else if (countType != v.cType)
                            {
                                LogMgr.Instance.Log("CountType not match in Node: " + Renderer.UITitle + "." + vName);
                                bRes = false;
                            }
                        }
                    }
                }

                if (TypeMap.Items.Count > 0)
                {
                    Variable vCache = null;
                    foreach (var mapItem in TypeMap.Items)
                    {
                        if (vCache == null || vCache.Name != mapItem.SrcVariable)
                            vCache = memory.GetVariable(mapItem.SrcVariable);
                        if (vCache == null)
                            continue;
                        if (vCache.Value != mapItem.SrcValue)
                            continue;
                        Variable des = memory.GetVariable(mapItem.DesVariable);
                        if (des == null)
                            continue;

                        if (mapItem.DesVType != Variable.ValueType.VT_NONE && mapItem.DesVType != des.vType)
                        {
                            LogMgr.Instance.Log("Value Type not match in Node: " + Renderer.UITitle + "." + mapItem.DesVariable);
                            bRes = false;
                        }

                        if (mapItem.DesCType != Variable.CountType.CT_NONE && mapItem.DesCType != des.cType)
                        {
                            LogMgr.Instance.Log("Count Type not match in Node: " + Renderer.UITitle + "." + mapItem.DesVariable);
                            bRes = false;
                        }
                    }
                }
            }

            return _OnCheckValid() && bRes;
        }

        protected virtual bool _OnCheckValid()
        {
            return true;
        }

        public override void OnAddToGraph()
        {
            base.OnAddToGraph();
            foreach (VariableHolder v in Variables.Datas)
            {
                v.Variable.RefreshCandidates();
            }
        }

        public override bool Disabled
        {
            get => base.Disabled;
            set
            {
                int newValue = value ? 1 : 0;
                if (m_DisableCount == newValue)
                    return;

                m_SelfDisabled = newValue;
                PropertyChange(RenderProperty.Disabled);
                if (value)
                    Utility.OperateNode(this, true, _IncreaseDisable);
                else
                    Utility.OperateNode(this, true, _DecreaseDisable);

                if (Graph != null)
                    Graph.RefreshNodeUID(0);
            }
        }

        static void _IncreaseDisable(NodeBase node)
        {
            TreeNode treeNode = node as TreeNode;
            ++treeNode.m_DisableCount;
            treeNode.PropertyChange(RenderProperty.Disabled);
        }
        static void _DecreaseDisable(NodeBase node)
        {
            TreeNode treeNode = node as TreeNode;
            if (treeNode.m_DisableCount > 0)
            {
                --treeNode.m_DisableCount;
                treeNode.PropertyChange(RenderProperty.Disabled);
            }
        }

        /// <summary>
        /// When a node is attached to a disabled parent, all children should be set to disabled, and vice versa.
        /// </summary>
        public override void OnConnectFromChanged()
        {
            base.OnConnectFromChanged();
            TreeNode parent = Parent;
            if ((parent == null && m_DisableCount - m_SelfDisabled > 0) || (parent != null && parent.Disabled))
                Utility.OperateNode(this, true, _SetDisableBasedOnParent);

        }
        static void _SetDisableBasedOnParent(NodeBase node)
        {
            TreeNode treeNode = node as TreeNode;
            TreeNode parent = (node as TreeNode).Parent;
            if (parent == null)
                treeNode.m_DisableCount = treeNode.m_SelfDisabled;
            else
                treeNode.m_DisableCount = treeNode.m_SelfDisabled + parent.m_DisableCount;
        }

        public override void OnConnectToChanged()
        {
            base.OnConnectToChanged();
            Conns.Sort(Connections.SortByPosX);
        }

        public void OnVariableValueChanged(Variable v)
        {
            if (TypeMap.TryGet(v, out TypeMap.Item item))
            {
                Variable des = this.Variables.GetVariable(item.DesVariable);
                if (des != null)
                {
                    if (item.DesCType != Variable.CountType.CT_NONE)
                        des.cType = item.DesCType;
                    if (item.DesVType != Variable.ValueType.VT_NONE)
                        des.vType = item.DesVType;
                }
            }
            _OnVariableValueChanged(v);
            PropertyChange(RenderProperty.Note);
        }
        protected virtual void _OnVariableValueChanged(Variable v)
        {

        }

        #region CONDITION
        Connector m_ConditonConnector;
        bool m_bEnableConditionConnection = false;
        public bool HasConditionConnection { get { return m_ConditonConnector.Conns.Count > 0; } }
        public bool EnableCondition
        {
            get { return m_bEnableConditionConnection; }
            set
            {
                m_bEnableConditionConnection = value;
            }
        }
        #endregion

    }
}
