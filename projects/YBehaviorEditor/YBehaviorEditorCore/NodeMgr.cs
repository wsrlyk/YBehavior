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
                node.CreateVariables();
                node.CreateRenderer();
                return node;
            }

            node = ExternalActionMgr.Instance.GetNode(name);
            if (node != null)
            {
                node.CreateRenderer();
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

        NH_Sequence = 13,
        NH_Selector = 23,
        
        NH_DefaultAction = 11,
        NH_Custom = 21,
    }

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
        protected NodeBase m_Parent;

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
                m_NickName = value;
                OnPropertyChanged("UITitle");
            }
        }
        public string UITitle
        {
            get { return UID.ToString() + ". " + FullName; }
        }

        public virtual string Note => string.Empty;
        public virtual string Icon => Connection.IdentifierParent;

        string m_Comment = string.Empty;// "This is a node comment test.";
        public string Comment
        {
            get { return m_Comment; }
            set
            {
                m_Comment = value;
                OnPropertyChanged("Comment");
            }
        }

        protected NodeType m_Type = NodeType.NT_Invalid;
        public NodeType Type { get { return m_Type; } set { m_Type = value; } }
        protected NodeHierachy m_Hierachy = NodeHierachy.NH_None;
        public NodeHierachy Hierachy { get { return m_Hierachy; } set { m_Hierachy = value; } }

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

        protected SharedData m_Variables;
        public SharedData Variables { get { return m_Variables; } }

        public SharedData SharedData { get { return GetTreeSharedData(); } }
        protected SharedData m_TreeSharedData = null;
        public SharedData GetTreeSharedData()
        {
            if (m_TreeSharedData != null)
                return m_TreeSharedData;
            Tree root = Root as Tree;
            if (root != null)
                m_TreeSharedData = root.Variables;
            else
            {
                Tree globalTree = Tree.GlobalTree;
                if (globalTree != null)
                    return globalTree.Variables;
            }
            return m_TreeSharedData;
        }

        public Node()
        {
            if (_HasParentHolder())
                m_Connections.CreateParentHolder(this);

            m_Variables = new SharedData(this);
            m_Renderer = new Renderer(this);
        }

        public virtual void Load(System.Xml.XmlNode data)
        {
            foreach (System.Xml.XmlAttribute attr in data.Attributes)
            {
                if (ReservedAttributes.Contains(attr.Name))
                    continue;
                ProcessAttrWhenLoad(attr);
            }
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

        public virtual void CreateVariables()
        {

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
            foreach (var v in m_Variables.Datas.Values)
            {
                v.RefreshCandidates(true);
            }
        }
        public virtual void Save(System.Xml.XmlElement data)
        {
            if (Conns.ParentHolder != null && Conns.ParentHolder.Conn != null)
            {
                if (Conns.ParentHolder.Conn.Identifier != Connection.IdentifierChildren)
                    data.SetAttribute("Connection", Conns.ParentHolder.Conn.Identifier);
            }

            data.SetAttribute("Pos", Renderer.Geo.Pos.ToString());
            if (!string.IsNullOrEmpty(m_NickName))
                data.SetAttribute("NickName", m_NickName);

            if (!DebugPointInfo.NoDebugPoint)
                data.SetAttribute("DebugPoint", DebugPointInfo.HitCount.ToString());

            if (!string.IsNullOrEmpty(Comment))
            {
                data.SetAttribute("Comment", Comment);
            }

            foreach (Variable v in Variables.Datas.Values)
            {
                data.SetAttribute(v.Name, v.ValueInXml);
            }
        }

        public virtual void Export(System.Xml.XmlElement data)
        {
            if (Conns.ParentHolder != null && Conns.ParentHolder.Conn != null)
            {
                if (Conns.ParentHolder.Conn.Identifier != Connection.IdentifierChildren)
                    data.SetAttribute("Connection", Conns.ParentHolder.Conn.Identifier);
            }

            foreach (Variable v in Variables.Datas.Values)
            {
                data.SetAttribute(v.Name, v.ValueInXml);
            }
        }

        public virtual Renderer CreateRenderer()
        {
            m_Renderer = new Renderer(this);
            return m_Renderer;
        }

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
            if (m_Variables.SameTypeGroup == null)
                return true;
            bool bRes = true;
            foreach (HashSet<string> group in m_Variables.SameTypeGroup)
            {
                Variable.ValueType valueType = Variable.ValueType.VT_NONE;
                foreach (string vName in group)
                {
                    Variable v = m_Variables.GetVariable(vName);
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

            return bRes;
        }

        public virtual Node Clone()
        {
            Node other = Activator.CreateInstance(this.GetType()) as Node;
            other.m_Name = this.m_Name;
            other.m_Type = this.m_Type;
            other.m_Hierachy = this.m_Hierachy;
            other.m_NickName = this.m_NickName;
            other.Renderer.Geo.Copy(this.Renderer.Geo);
            other.Renderer.Geo.Pos = other.Renderer.Geo.Pos + new Vector(5, 5);
            other.m_TreeSharedData = this.m_TreeSharedData;

            foreach (var v in m_Variables.Datas.Values)
            {
                Variable newv = v.Clone(other);
                other.Variables.AddVariable(newv);
            }
            return other;
        }

        public void Delete(int param)
        {
            ///> Check if is root
            if (Type == NodeType.NT_Root)
                return;

            ///> Disconnect all the connection
            NodesDisconnectedArg arg = new NodesDisconnectedArg();
            arg.ChildHolder = Conns.ParentHolder;
            EventMgr.Instance.Send(arg);

            foreach (var child in Conns)
            {
                Node chi = child as Node;
                if (chi == null)
                    continue;
                arg.ChildHolder = chi.Conns.ParentHolder;
                EventMgr.Instance.Send(arg);

                if (param != 0)
                    chi.Delete(param);
            }

            RemoveNodeArg removeArg = new RemoveNodeArg();
            removeArg.Node = this;
            EventMgr.Instance.Send(removeArg);
        }

        public void SetDebugPoint(int count)
        {
            DebugPointInfo.HitCount = count;
            OnPropertyChanged("DebugPointInfo");
            DebugMgr.Instance.SetDebugPoint(UID, count);
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
}
