using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;

namespace YBehavior.Editor.Core
{
    public class NodeMgr : Singleton<NodeMgr>
    {
        List<Node> m_NodeList = new List<Node>();
        public List<Node> NodeList { get { return m_NodeList; } }
        private Dictionary<string, Type> m_TypeDic = new Dictionary<string, Type>();

        public NodeMgr()
        {
            var subTypeQuery = from t in System.Reflection.Assembly.GetExecutingAssembly().GetTypes()
                               where IsSubClassOf(t, typeof(Node))
                               select t;

            foreach (var type in subTypeQuery)
            {
                Node node = Activator.CreateInstance(type) as Node;
                if (node.Type == NodeType.NT_Invalid)
                    continue;
                node.LoadDescription();
                m_NodeList.Add(node);
                m_TypeDic.Add(node.Name, type);
                Console.WriteLine(type);
            }
        }

        public Node CreateNodeByName(string name)
        {
            Node node = null;
            if (m_TypeDic.TryGetValue(name, out Type type))
            {
                node = Activator.CreateInstance(type) as Node;
                node.CreateBase();
                node.CreateVariables();
                node.LoadDescription();
                //node.CreateRenderer();
                return node;
            }

            node = ExternalActionMgr.Instance.GetNode(name);
            if (node != null)
            {
                //node.CreateRenderer();
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

    public enum NodeType
    {
        NT_Invalid,
        NT_Root,
        NT_Default,
        NT_External
    }

    public enum NodeHierachy
    {
        NH_None,
        NH_Action = 1,
        NH_Decorator = 2,
        NH_Compositor = 3,
        NH_Array = 4,

        NH_DefaultAction = 11,
        NH_Custom = 21,
    }
    #region NodeBase
    public class NodeBase
    {
        protected Connections m_Connections = new Connections();
        public Connections Conns { get { return m_Connections; } }

        public Connection ParentConn
        {
            get
            {
                if (m_Connections.ParentHolder == null)
                    return null;
                return m_Connections.ParentHolder.Conn;
            }
        }

        public NodeBase Parent
        {
            get
            {
                Connection parentConn = ParentConn;
                if (parentConn == null)
                    return null;
                return parentConn.Owner;
            }
        }

        public NodeBase Root
        {
            get
            {
                NodeBase root = this;
                NodeBase parent = this;
                while (parent != null)
                {
                    root = parent;
                    parent = parent.Parent;
                }
                return root;
            }
        }

        public virtual void OnChildChanged()
        {

        }
        public virtual void OnParentChanged()
        {

        }
    }
    #endregion

    #region Geometry
    public class Geometry
    {
        Rect m_Rect;
        public Rect Rec { get { return m_Rect; } set { m_Rect = value; } }
        public Thickness Thick { get { return new Thickness(m_Rect.Left, m_Rect.Top, m_Rect.Right, m_Rect.Bottom); } }

        public Geometry()
        {
            m_Rect = new Rect(0, 0, 80, 60);
        }

        public Point CenterPoint
        {
            get
            {
                return new Point((m_Rect.Left + m_Rect.Right) / 2, (m_Rect.Top + m_Rect.Bottom) / 2);
            }
        }
        public Point TopPoint
        {
            get
            {
                return new Point((m_Rect.Left + m_Rect.Right) / 2, m_Rect.Top);
            }
        }
        public Point BottomPoint
        {
            get
            {
                return new Point((m_Rect.Left + m_Rect.Right) / 2, m_Rect.Bottom);
            }
        }

        public Point Pos
        {
            get { return m_Rect.Location; }
            set { m_Rect.Location = value; }
        }

        public Point BottomRightPos
        {
            get { return m_Rect.BottomRight; }
            set
            {
                Point p = value;
                if (p.X < m_Rect.X)
                    p.X = m_Rect.X;
                if (p.Y < m_Rect.Y)
                    p.Y = m_Rect.Y;
                m_Rect.Width = p.X - m_Rect.X;
                m_Rect.Height = p.Y - m_Rect.Y;
            }
        }


        public Point TopLeftPos
        {
            get { return m_Rect.TopLeft; }
            set
            {
                Point p = value;
                if (p.Y > m_Rect.Y + m_Rect.Height)
                    p.Y = m_Rect.Y + m_Rect.Height;
                if (p.X > m_Rect.X + m_Rect.Width)
                    p.X = m_Rect.X + m_Rect.Width;
                m_Rect.Height = m_Rect.Y + m_Rect.Height - p.Y;
                m_Rect.Y = p.Y;
                m_Rect.Width = m_Rect.X + m_Rect.Width - p.X;
                m_Rect.X = p.X;
            }
        }

        public Point TopRightPos
        {
            get { return m_Rect.TopRight; }
            set
            {
                Point p = value;
                if (p.X < m_Rect.X)
                    p.X = m_Rect.X;
                if (p.Y > m_Rect.Y + m_Rect.Height)
                    p.Y = m_Rect.Y + m_Rect.Height;
                m_Rect.Height = m_Rect.Y + m_Rect.Height - p.Y;
                m_Rect.Y = p.Y;
                m_Rect.Width = p.X - m_Rect.X;
            }
        }

        public Point BottomLeftPos
        {
            get { return m_Rect.BottomLeft; }
            set
            {
                Point p = value;
                if (p.Y < m_Rect.Y)
                    p.Y = m_Rect.Y;
                if (p.X > m_Rect.X + m_Rect.Width)
                    p.X = m_Rect.X + m_Rect.Width;
                m_Rect.Width = m_Rect.X + m_Rect.Width - p.X;
                m_Rect.X = p.X;
                m_Rect.Height = p.Y - m_Rect.Y;
            }
        }
        public void Copy(Geometry other)
        {
            m_Rect = other.m_Rect;
        }
    }
    #endregion
    public class Node : NodeBase, System.ComponentModel.INotifyPropertyChanged, IVariableDataSource
    {
        protected string m_Name;
        protected string m_NickName;
        public virtual string Name { get { return m_Name; } }
        public string FullName
        {
            get { return string.IsNullOrEmpty(m_NickName) ? Name : m_NickName; }
        }
        public string NickName
        {
            get { return m_NickName; }
            set
            {
                ChangeNodeNickNameCommand command = new ChangeNodeNickNameCommand()
                {
                    Node = this,
                    OriginNickName = m_NickName,
                    FinalNickName = value,
                };

                m_NickName = value;
                OnPropertyChanged("UITitle");

                WorkBenchMgr.Instance.PushCommand(command);
            }
        }
        public string UITitle
        {
            get { return UID.ToString() + ". " + FullName; }
        }

        public virtual string Note => string.Empty;
        public virtual string Icon => Connection.IdentifierParent;

        NodeDescription m_NodeDescripion;
        public string Description => m_NodeDescripion == null ? string.Empty : m_NodeDescripion.node;

        string m_Comment = string.Empty;// "This is a node comment test.";
        public string Comment
        {
            get { return m_Comment; }
            set
            {
                ChangeNodeCommentCommand command = new ChangeNodeCommentCommand()
                {
                    Node = this,
                    OriginComment = m_Comment,
                    FinalComment = value,
                };

                m_Comment = value;
                OnPropertyChanged("Comment");

                WorkBenchMgr.Instance.PushCommand(command);
            }
        }

        bool m_Folded = false;
        public bool Folded
        {
            get { return m_Folded; }
            set
            {
                if (m_Folded != value)
                {
                    m_Folded = value;

                    if (value)
                        WorkBenchMgr.Instance.RemoveRenderers(this, true);
                    else
                        WorkBenchMgr.Instance.AddRenderers(this, false, true);

                    OnPropertyChanged("Folded");
                }
            }
        }
        public bool IsChildrenRendering { get { return !Folded; } }

        #region DISABLE
        int m_DisableCount = 0;
        int m_SelfDisabled = 0;
        public bool Disabled
        {
            get { return m_DisableCount > 0; }
            set
            {
                int newValue = value ? 1 : 0;
                if (m_SelfDisabled == newValue)
                    return;

                ChangeNodeDisableCommand command = new ChangeNodeDisableCommand()
                {
                    Node = this,
                    OriginState = SelfDisabled,
                };

                m_SelfDisabled = newValue;
                if (value)
                    Utility.OperateNode(this, true, _IncreaseDisable);
                else
                    Utility.OperateNode(this, true, _DecreaseDisable);

                WorkBenchMgr.Instance.RefreshNodeUID();

                WorkBenchMgr.Instance.PushCommand(command);
            }
        }

        public bool SelfDisabled { get { return m_SelfDisabled > 0; } }
        public int DisableCount { get { return m_DisableCount; } }

        static void _IncreaseDisable(Node node)
        {
            ++node.m_DisableCount;
            node.OnPropertyChanged("Disabled");
        }
        static void _DecreaseDisable(Node node)
        {
            if (node.m_DisableCount > 0)
            {
                --node.m_DisableCount;
                node.OnPropertyChanged("Disabled");
            }
        }
        static void _SetDisableBasedOnParent(Node node)
        {
            Node parent = node.Parent as Node;
            if (parent == null)
                node.m_DisableCount = node.m_SelfDisabled;
            else
                node.m_DisableCount = node.m_SelfDisabled + parent.DisableCount;
        }
        #endregion

        protected NodeType m_Type = NodeType.NT_Invalid;
        public NodeType Type { get { return m_Type; } set { m_Type = value; } }
        protected NodeHierachy m_Hierachy = NodeHierachy.NH_None;
        public NodeHierachy Hierachy { get { return m_Hierachy; } set { m_Hierachy = value; } }

        private string m_ReturnType = "Default";
        public string ReturnType
        {
            get { return m_ReturnType; }
            set
            {
                m_ReturnType = value;
                OnPropertyChanged("ReturnType");
            }
        } 

        public static readonly HashSet<string> ReservedAttributes = new HashSet<string>(new string[] { "Class", "Connection" });
        public static readonly HashSet<string> ReservedAttributesAll = new HashSet<string>(new string[] { "Class", "Pos", "NickName", "Connection", "DebugPoint", "Comment" });

        public Renderer Renderer { get { return m_Renderer; } }
        protected Renderer m_Renderer;

        private uint m_UID = 0;
        public uint UID
        {
            get { return m_UID; }
            set
            {
                m_UID = value;
                OnPropertyChanged("UITitle");
            }
        }

        public DebugPointInfo DebugPointInfo { get; } = new DebugPointInfo();

        protected TreeMemory m_TreeMemory;
        public TreeMemory TreeMemory { get { return m_TreeMemory; } }
        protected NodeMemory m_NodeMemory;
        public NodeMemory NodeMemory { get { return m_NodeMemory; } }
        protected IVariableCollection m_Variables;
        public IVariableCollection Variables { get { return m_Variables; } }

        public TreeMemory SharedData { get { return GetTreeSharedData(); } }
        protected TreeMemory m_TreeSharedData = null;
        public TreeMemory GetTreeSharedData()
        {
            if (m_TreeSharedData != null)
                return m_TreeSharedData;
            Tree root = Root as Tree;
            if (root != null)
                m_TreeSharedData = root.TreeMemory;
            else
            {
                Tree globalTree = Tree.GlobalTree;
                if (globalTree != null)
                    return globalTree.TreeMemory;
            }
            return m_TreeSharedData;
        }
        public virtual InOutMemory InOutData { get { return null; } }

        TypeMap m_TypeMap = new TypeMap();
        public TypeMap TypeMap { get { return m_TypeMap; } }

        #region CONDITION
        Connection m_ConditonConnection;
        bool m_bEnableConditionConnection = false;
        public bool HasConditionConnection { get { return m_ConditonConnection.NodeCount > 0; } }
        public bool EnableCondition
        {
            get { return m_bEnableConditionConnection; }
            set
            {
                if (value == false)
                {
                    ///> Check if has connection
                    if (m_ConditonConnection.NodeCount > 0)
                    {
                        ShowSystemTipsArg showSystemTipsArg = new ShowSystemTipsArg()
                        {
                            Content = "Should remove connection first.",
                            TipType = ShowSystemTipsArg.TipsType.TT_Error,
                        };
                        EventMgr.Instance.Send(showSystemTipsArg);
                        return;
                    }
                }
                m_bEnableConditionConnection = value;
                OnPropertyChanged("EnableCondition");
            }
        }
        #endregion

        public Node()
        {
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
                    Renderer.Geo.Pos = Point.Parse(attr.Value);
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
                    ReturnType = attr.Value;
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
            Variable v =  Variables.GetVariable(attr.Name);
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

        public void Init()
        {
            OnInit();
        }

        public void CreateBase()
        {
            if (_HasParentHolder())
                m_Connections.CreateParentHolder(this);

            if (this is Tree)
            {
                m_Variables = new TreeMemory(this);
                m_TreeMemory = m_Variables as TreeMemory;
            }
            else
            {
                m_Variables = new NodeMemory(this);
                m_NodeMemory = m_Variables as NodeMemory;
            }

            m_Renderer = new Renderer(this);
            if (m_Renderer is Renderer) { }
            m_ConditonConnection = new ConnectionSingle(this, Connection.IdentifierCondition);

            _OnCreateBase();
        }

        protected virtual void _OnCreateBase()
        {

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
        protected virtual void OnInit()
        {
            m_TreeSharedData = null;
            _InitVariables();
        }

        public override void OnChildChanged()
        {
            base.OnChildChanged();
            Conns.Sort(SortByPosX);
        }

        protected void _InitVariables()
        {
            foreach (var v in m_Variables.Datas)
            {
                v.Variable.RefreshCandidates(true);
            }
        }
        public void Save(System.Xml.XmlElement data, System.Xml.XmlDocument xmlDoc)
        {
            if (Conns.ParentHolder != null && Conns.ParentHolder.Conn != null)
            {
                if (Conns.ParentHolder.Conn.Identifier != Connection.IdentifierChildren)
                    data.SetAttribute("Connection", Conns.ParentHolder.Conn.Identifier);
            }

            if (ReturnType != "Default")
                data.SetAttribute("Return", ReturnType);

            Point intPos = new Point((int)Renderer.Geo.Pos.X, (int)Renderer.Geo.Pos.Y);
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
            if (Conns.ParentHolder != null && Conns.ParentHolder.Conn != null)
            {
                if (Conns.ParentHolder.Conn.Identifier != Connection.IdentifierChildren)
                    data.SetAttribute("Connection", Conns.ParentHolder.Conn.Identifier);
            }

            if (ReturnType != "Default")
                data.SetAttribute("Return", ReturnType);

            _OnExportVariables(data, xmlDoc);
        }
        protected virtual void _OnExportVariables(System.Xml.XmlElement data, System.Xml.XmlDocument xmlDoc)
        {
            _WriteVariables(Variables, data);
        }

        //public virtual Renderer CreateRenderer()
        //{
        //    m_Renderer = new Renderer(this);
        //    return m_Renderer;
        //}

        protected virtual bool _HasParentHolder()
        {
            return true;
        }

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        internal protected void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
            }
        }

        public void OnChildPosChanged()
        {
            Conns.Sort(SortByPosX);
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
                                LogMgr.Instance.Log("ValueType not match in Node: " + UITitle + "." + vName);
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
                            LogMgr.Instance.Log("Value Type not match in Node: " + UITitle + "." + mapItem.DesVariable);
                            bRes = false;
                        }

                        if (mapItem.DesCType != Variable.CountType.CT_NONE && mapItem.DesCType != des.cType)
                        {
                            LogMgr.Instance.Log("Count Type not match in Node: " + UITitle + "." + mapItem.DesVariable);
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

        public virtual Node Clone()
        {
            Node other = Activator.CreateInstance(this.GetType()) as Node;
            other.CreateBase();
            other.m_Name = this.m_Name;
            other.m_Type = this.m_Type;
            other.m_Hierachy = this.m_Hierachy;
            other.m_NickName = this.m_NickName;
            other.Renderer.Geo.Copy(this.Renderer.Geo);
            other.Renderer.Geo.Pos = other.Renderer.Geo.Pos + new Vector(5, 5);
            other.m_TreeSharedData = this.m_TreeSharedData;

            if (this.Variables is NodeMemory)
            {
                (other.Variables as NodeMemory).CloneFrom(this.Variables as NodeMemory);
            }
            other.TypeMap.CloneFrom(this.TypeMap);
            return other;
        }

        public void Delete(int param)
        {
            ///> Check if is root
            if (Type == NodeType.NT_Root)
                return;

            ///> If folded but remove only this, unfold first
            if (Folded && param == 0)
                Folded = !Folded;

            ///> Disconnect all the connection
            WorkBenchMgr.Instance.DisconnectNodes(Conns.ParentHolder);

            foreach (var child in Conns)
            {
                Node chi = child as Node;
                if (chi == null)
                    continue;

                WorkBenchMgr.Instance.DisconnectNodes(chi.Conns.ParentHolder);

                if (param != 0)
                    chi.Delete(param);
            }

            WorkBenchMgr.Instance.RemoveNode(this);
        }

        public void SetDebugPoint(int count)
        {
            DebugPointInfo.HitCount = count;
            OnPropertyChanged("DebugPointInfo");
            DebugMgr.Instance.SetDebugPoint(UID, count);
        }

        public override void OnParentChanged()
        {
            base.OnParentChanged();
            Node parent = Parent as Node;
            if ((parent == null && m_DisableCount - m_SelfDisabled > 0) || (parent != null && parent.Disabled))
                Utility.OperateNode(this, true, _SetDisableBasedOnParent);
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
            OnPropertyChanged("Note");
        }

        public static int SortByPosX(NodeBase aa, NodeBase bb)
        {
            Node a = aa as Node;
            Node b = bb as Node;
            if (a == null || b == null)
                return 0;

            return a.Renderer.Geo.Pos.X.CompareTo(b.Renderer.Geo.Pos.X);
        }
    }

    #region BASIC NODE
    public class BranchNode : Node
    {
        protected Connection m_ChildConn;
        public Connection ChildConn { get { return m_ChildConn; } }
    }

    public class LeafNode : Node
    {
        public LeafNode()
        {
            new ConnectionNone(this, Connection.IdentifierChildren);
        }
    }

    public class SingleChildNode : BranchNode
    {
        public SingleChildNode()
        {
            m_ChildConn = new ConnectionSingle(this, Connection.IdentifierChildren);
        }
    }

    public class CompositeNode : BranchNode
    {
        public CompositeNode()
        {
            m_ChildConn = new ConnectionMultiple(this, Connection.IdentifierChildren);
        }
    }
    #endregion
}
