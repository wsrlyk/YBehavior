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
            if (m_TypeDic.TryGetValue(name, out Type type))
            {
                Node node = Activator.CreateInstance(type) as Node;
                node.CreateRenderer();
                return node;
            }
            return null;
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
        NT_Sequence,
        NT_Calculator,
        NT_Not,
        NT_AlwaysSuccess,
        NT_Selector,
    }

    public enum NodeHierachy
    {
        NH_None,
        NH_Action = 1,
        NH_Decorator = 2,
        NH_Compositor = 3,

        NH_Sequence = 13,
        NH_Selector = 23,
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

    }

    public class Node : NodeBase
    {
        protected string m_Name;
        protected string m_NickName;
        public string Name { get { return m_Name; } }
        public string NickName { get { return m_NickName == null ? m_Name : m_NickName; } }

        protected NodeType m_Type = NodeType.NT_Invalid;
        public NodeType Type { get { return m_Type; } }
        protected NodeHierachy m_Hierachy = NodeHierachy.NH_None;
        public NodeHierachy Hierachy { get { return m_Hierachy; } }

        public static readonly HashSet<string> ReservedAttributes = new HashSet<string>(new string[] { "Class" });

        public Renderer Renderer { get { return m_Renderer; } }
        protected Renderer m_Renderer;

        private Geometry m_Geo = new Geometry();
        public Geometry Geo { get { return m_Geo; } }

        protected SharedData m_Variables = new SharedData();
        public SharedData Variables { get { return m_Variables; } }

        protected SharedData m_TreeSharedData = null;
        public SharedData GetTreeSharedData()
        {
            if (m_TreeSharedData != null)
                return m_TreeSharedData;
            Tree root = Root as Tree;
            if (root != null)
                m_TreeSharedData = root.Variables;
            return m_TreeSharedData;
        }

        public class Geometry
        {
            Rect m_Rect;
            public Rect Rec { get { return m_Rect; } }
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
        }

        public Node()
        {
            if (_HasParentHolder())
                m_Connections.CreateParentHolder(this);
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

        protected virtual bool ProcessAttrWhenLoad(System.Xml.XmlAttribute attr)
        {
            switch (attr.Name)
            {
                case "Pos":
                    m_Geo.Pos = Point.Parse(attr.Value);
                    break;
                case "NickName":
                    m_NickName = attr.Value;
                    break;
                default:
                    return false;
            }

            return true;
        }

        public virtual void Save(System.Xml.XmlElement data)
        {
            if (Conns.ParentHolder != null && Conns.ParentHolder.Conn != null)
            {
                if (Conns.ParentHolder.Conn.Identifier != Connection.IdentifierChildren)
                    data.SetAttribute("Connection", Conns.ParentHolder.Conn.Identifier);
            }

            data.SetAttribute("Pos", m_Geo.Pos.ToString());
            if (m_NickName != null)
                data.SetAttribute("NickName", m_NickName);

            foreach (Variable v in Variables.Datas)
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
