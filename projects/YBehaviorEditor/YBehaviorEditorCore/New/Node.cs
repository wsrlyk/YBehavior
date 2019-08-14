using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace YBehavior.Editor.Core.New
{
    public class NodeBase
    {
        private uint m_UID = 0;
        protected string m_Name = "";
        protected string m_NickName;
        protected string m_Comment = string.Empty;// "This is a node comment test.";
        protected int m_DisableCount = 0;
        protected NodeDescription m_NodeDescripion;

        public DebugPointInfo DebugPointInfo { get; } = new DebugPointInfo();

        public virtual string Name { get { return m_Name; } }
        public string NickName
        {
            get { return m_NickName; }
            set
            {
                m_NickName = value;
                PropertyChange(RenderProperty.NickName);
            }
        }
        public string Comment
        {
            get { return m_Comment; }
            set
            {
                m_Comment = value;
                PropertyChange(RenderProperty.Comment);
            }
        }
        public uint UID
        {
            get { return m_UID; }
            set
            {
                m_UID = value;
                PropertyChange(RenderProperty.UID);
            }
        }

        public virtual bool SelfDisabled { get { return m_DisableCount > 0; } }
        public virtual bool Disabled
        {
            get { return m_DisableCount > 0; }
            set
            {
                int newValue = value ? 1 : 0;
                if (m_DisableCount == newValue)
                    return;

                m_DisableCount = newValue;
                PropertyChange(RenderProperty.Disable);

                Graph.RefreshNodeUID();
            }
        }

        public virtual string Note => string.Empty;
        public virtual string Icon => Connector.IdentifierParent;
        public string Description => m_NodeDescripion == null ? string.Empty : m_NodeDescripion.node;
        public Graph Graph { get; set; }
        public Geometry Geo { get; } = new Geometry();

        protected NodeBaseRenderer m_Renderer;
        public NodeBaseRenderer Renderer { get { return m_Renderer; } }
        public NodeBaseRenderer ForceGetRenderer
        {
            get
            {
                if (m_Renderer == null)
                    _CreateRenderer();
                return m_Renderer;
            }
        }
        protected virtual void _CreateRenderer()
        {
            m_Renderer = new NodeBaseRenderer(this);
        }
        protected Connections m_Connections;
        public Connections Conns { get { return m_Connections; } }

        public NodeBase()
        {
            m_Connections = new Connections(this);
        }

        public void SetDebugPoint(int count)
        {
            DebugPointInfo.HitCount = count;
            PropertyChange(RenderProperty.DebugPoint);
            DebugMgr.Instance.SetDebugPoint(UID, count);
        }

        public virtual void CreateBase() { }
        /// <summary>
        /// Parent changed
        /// </summary>
        public virtual void OnConnectFromChanged()
        {

        }

        /// <summary>
        /// Child changed
        /// </summary>
        public virtual void OnConnectToChanged()
        {

        }

        protected void PropertyChange(RenderProperty property)
        {
            if (m_Renderer != null)
                m_Renderer.PropertyChange(property);
        }

        public virtual NodeBase Clone()
        {
            NodeBase other = Activator.CreateInstance(this.GetType()) as NodeBase;
            return other;
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
                               where IsSubClassOf(t, typeof(TreeNode))
                               select t;

            foreach (var type in subTypeQuery)
            {
                TreeNode node = Activator.CreateInstance(type) as TreeNode;
                if (node.Type == TreeNodeType.TNT_Invalid)
                    continue;
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

        static bool IsSubClassOf(Type type, Type baseType)
        {
            var b = type.BaseType;
            while (b != null)
            {
                if (b.Equals(baseType))
                {
                    return true;
                }
                b = b.BaseType;
            }
            return false;
        }
    }

    public class TreeNode : NodeBase
    {
        private string m_ReturnType = "Default";
        protected bool m_Folded = false;
        protected int m_SelfDisabled = 0;
        public override bool SelfDisabled { get { return m_SelfDisabled > 0; } }
        public TreeNodeType Type { get; set; }
        public NodeHierachy Hierachy { get; set; }
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

        public static readonly HashSet<string> ReservedAttributes = new HashSet<string>(new string[] { "Class", "Connection" });
        public static readonly HashSet<string> ReservedAttributesAll = new HashSet<string>(new string[] { "Class", "Pos", "NickName", "Connection", "DebugPoint", "Comment" });

        public TreeNode()
        {
        }

        public override void CreateBase()
        {
            base.CreateBase();
            if (Type != TreeNodeType.TNT_Root)
                Conns.Add(Connector.IdentifierParent, false);

            if (Type == TreeNodeType.TNT_Root)
            {
                m_Variables = new TreeMemory(this);
            }
            else
            {
                m_Variables = new NodeMemory(this);
                m_NodeMemory = m_Variables as NodeMemory;
            }
            m_ConditonConnector = Conns.Add(Connector.IdentifierCondition, false);
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
                return m_Connections.ParentConnector.Conns[0].From.Owner as TreeNode;
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
                case "DebugPoint":
                    DebugPointInfo.HitCount = int.Parse(attr.Value);
                    break;
                case "Comment":
                    Comment = attr.Value;
                    break;
                case "Disabled":
                    Disabled = bool.Parse(attr.Value);
                    break;
                case "Folded":
                    m_Folded = bool.Parse(attr.Value);
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
                if (m_Connections.ParentConnector.Conns[0].From.Identifier != Connector.IdentifierChildren)
                    data.SetAttribute("Connection", m_Connections.ParentConnector.Conns[0].From.Identifier);
            }

            if (ReturnType != "Default")
                data.SetAttribute("Return", ReturnType);

            Point intPos = new Point((int)Geo.Pos.X, (int)Geo.Pos.Y);
            data.SetAttribute("Pos", intPos.ToString());
            if (!string.IsNullOrEmpty(m_NickName))
                data.SetAttribute("NickName", m_NickName);

            if (!DebugPointInfo.NoDebugPoint)
                data.SetAttribute("DebugPoint", DebugPointInfo.HitCount.ToString());

            if (!string.IsNullOrEmpty(Comment))
            {
                data.SetAttribute("Comment", Comment);
            }

            if (SelfDisabled)
            {
                data.SetAttribute("Disabled", "true");
            }

            if (Folded)
            {
                data.SetAttribute("Folded", "true");
            }

            _OnSaveVariables(data, xmlDoc);
        }

        protected void _WriteVariables(IVariableCollection collection, System.Xml.XmlElement data)
        {
            foreach (VariableHolder v in collection.Datas)
            {
                data.SetAttribute(v.Variable.Name, v.Variable.ValueInXml);
            }
        }
        protected virtual void _OnSaveVariables(System.Xml.XmlElement data, System.Xml.XmlDocument xmlDoc)
        {
            _WriteVariables(Variables, data);
        }
        public void Export(System.Xml.XmlElement data, System.Xml.XmlDocument xmlDoc)
        {
            if (Conns.ParentConnector != null && Conns.ParentConnector.Conns.Count > 0)
            {
                if (m_Connections.ParentConnector.Conns[0].From.Identifier != Connector.IdentifierChildren)
                    data.SetAttribute("Connection", m_Connections.ParentConnector.Conns[0].From.Identifier);
            }

            if (ReturnType != "Default")
                data.SetAttribute("Return", ReturnType);

            _OnExportVariables(data, xmlDoc);
        }
        protected virtual void _OnExportVariables(System.Xml.XmlElement data, System.Xml.XmlDocument xmlDoc)
        {
            _WriteVariables(Variables, data);
        }

        public bool CheckValid()
        {
            bool bRes = true;
            if (m_Variables is NodeMemory)
            {
                NodeMemory memory = m_Variables as NodeMemory;
                SameTypeGroup sameTypeGroup = memory.SameTypeGroup;
                if (sameTypeGroup != null)
                {
                    foreach (HashSet<string> group in sameTypeGroup)
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

        public override bool Disabled
        {
            get => base.Disabled;
            set
            {
                int newValue = value ? 1 : 0;
                if (m_DisableCount == newValue)
                    return;

                m_SelfDisabled = newValue;
                PropertyChange(RenderProperty.Disable);
                if (value)
                    Utility.OperateNode(this, true, _IncreaseDisable);
                else
                    Utility.OperateNode(this, true, _DecreaseDisable);

                Graph.RefreshNodeUID();
            }
        }

        static void _IncreaseDisable(NodeBase node)
        {
            TreeNode treeNode = node as TreeNode;
            ++treeNode.m_DisableCount;
            treeNode.PropertyChange(RenderProperty.Disable);
        }
        static void _DecreaseDisable(NodeBase node)
        {
            TreeNode treeNode = node as TreeNode;
            if (treeNode.m_DisableCount > 0)
            {
                --treeNode.m_DisableCount;
                treeNode.PropertyChange(RenderProperty.Disable);
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

    public class RootTreeNode : SingleChildNode
    {
        public RootTreeNode() : base()
        {
            m_Name = "Root";
            Type = TreeNodeType.TNT_Root;
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
        public ActionTreeNode() : base()
        {
            Type = TreeNodeType.TNT_External;
        }

        public override NodeBase Clone()
        {
            ActionTreeNode other = base.Clone() as ActionTreeNode;
            return other;
        }
    }

    public class SequenceTreeNode : CompositeNode
    {
        public SequenceTreeNode() : base()
        {
            m_Name = "Sequence";
            Type = TreeNodeType.TNT_Default;
        }
    }

    public class SubTreeNode : LeafNode
    {
        public SubTreeNode() : base()
        {
            m_Name = "SubTree";
            Type = TreeNodeType.TNT_Default;
        }
    }
}
