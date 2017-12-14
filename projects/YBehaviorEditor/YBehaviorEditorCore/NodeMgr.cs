﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;

namespace YBehavior.Editor.Core
{
    public class NodeMgr
    {
        public static NodeMgr Instance { get { return s_Instance; } }
        static NodeMgr s_Instance = new NodeMgr();
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

        protected Connection m_ParentConnection;
        public Connection ParentConn
        {
            get { return m_ParentConnection; }
            set
            {
                m_ParentConnection = value;
                if (value != null)
                    m_Parent = m_ParentConnection.Owner;
                else
                    m_Parent = null;
            }
        }

        public NodeBase Parent { get { return m_Parent; } }
        protected NodeBase m_Parent;
    }

    public class Node : NodeBase
    {
        protected string m_Name;
        protected string m_NickName;
        public string Name { get { return m_Name; }}
        public string NickName { get { return m_NickName == null ? m_Name : m_NickName; } }

        protected NodeType m_Type = NodeType.NT_Invalid;
        public NodeType Type { get { return m_Type; }}
        protected NodeHierachy m_Hierachy = NodeHierachy.NH_None;
        public NodeHierachy Hierachy { get { return m_Hierachy; } }

        public Renderer Renderer { get { return m_Renderer; } }
        protected Renderer m_Renderer;

        private Geometry m_Geo = new Geometry();
        public Geometry Geo { get { return m_Geo; } }

        public class Geometry : System.ComponentModel.INotifyPropertyChanged
        {
            Rect m_Rect;
            public Rect Rec { get { return m_Rect; } }
            public Thickness Thick { get { return new Thickness(m_Rect.Left, m_Rect.Top, m_Rect.Right, m_Rect.Bottom); } }

            public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

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

            public double Width
            {
                get { return Rec.Width; }
                set
                {
                    m_Rect.Width = Math.Max(60, value);
                    if (PropertyChanged != null)
                    {
                        this.PropertyChanged.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs("Width"));
                    }
                }
            }

            public double Height
            {
                get { return Rec.Height; }
                set
                {
                    m_Rect.Height = Math.Max(80, value);
                    if (PropertyChanged != null)
                    {
                        this.PropertyChanged.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs("Height"));
                    }
                }
            }
        }

        public virtual void Load(System.Xml.XmlNode data)
        {
            foreach (System.Xml.XmlAttribute attr in data.Attributes)
            {
                switch (attr.Name)
                {
                    case "Pos":
                        m_Geo.Pos = Point.Parse(attr.Value);
                        break;
                    case "NickName":
                        m_NickName = attr.Value;
                        break;
                }
            }
        }

        public virtual Renderer CreateRenderer()
        {
            m_Renderer = new Renderer(this);
            return m_Renderer;
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
