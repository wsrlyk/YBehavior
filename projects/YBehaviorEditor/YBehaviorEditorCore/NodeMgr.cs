using System;
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
        List<NodeBase> m_NodeList = new List<NodeBase>();
        public List<NodeBase> NodeList { get { return m_NodeList; } }
        private Dictionary<string, Type> m_TypeDic = new Dictionary<string, Type>();

        public NodeMgr()
        {
            var subTypeQuery = from t in System.Reflection.Assembly.GetExecutingAssembly().GetTypes()
                               where IsSubClassOf(t, typeof(NodeBase))
                               select t;

            foreach (var type in subTypeQuery)
            {
                NodeBase node = Activator.CreateInstance(type) as NodeBase;
                if (node.Type == NodeType.NT_Invalid)
                    continue;
                m_NodeList.Add(node);
                m_TypeDic.Add(node.Name, type);
                Console.WriteLine(type);
            }
        }

        public NodeBase CreateNodeByName(string name)
        {
            if (m_TypeDic.TryGetValue(name, out Type type))
            {
                NodeBase node = Activator.CreateInstance(type) as NodeBase;
                node.Renderer = RenderersMgr.Instance.CreateRenderer(node.Type);
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
        protected string m_Name;
        public string Name { get { return m_Name; }}
        protected NodeType m_Type = NodeType.NT_Invalid;
        public NodeType Type { get { return m_Type; }}
        protected NodeHierachy m_Hierachy = NodeHierachy.NH_None;
        public NodeHierachy Hierachy { get { return m_Hierachy; } }
        public RendererBase Renderer { get; set; }
        private Point m_Pos;
        public Point Pos { get { return m_Pos; } }
        public NodeBase Parent { get; set; }

        public virtual void Load(System.Xml.XmlNode data)
        {
            foreach (System.Xml.XmlAttribute attr in data.Attributes)
            {
                if (attr.Name == "Pos")
                    m_Pos = Point.Parse(attr.Value);
            }
        }
        public virtual void AddChild(NodeBase node) { }
    }

    public class BranchNode : NodeBase
    {
        private List<NodeBase> m_Children = new List<NodeBase>();
        public List<NodeBase> Children { get { return m_Children; } }

        public override void AddChild(NodeBase node)
        {
            if (node == null)
                return;

            node.Parent = this;
            m_Children.Add(node);
        }
    }

    public class LeafNode : NodeBase
    {

    }

    public class SingleChildNode : BranchNode
    {

    }

    public class CompositeNode : BranchNode
    {

    }
}
