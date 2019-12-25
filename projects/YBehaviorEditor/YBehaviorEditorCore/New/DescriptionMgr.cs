using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace YBehavior.Editor.Core.New
{
    public class NodeDescription
    {
        Dictionary<string, string> variables = new Dictionary<string, string>();
        public string node;
        public void SetVariable(string name, string content)
        {
            variables[name] = content;
        }
        public string GetVariable(string name)
        {
            variables.TryGetValue(name, out string content);
            return content;
        }
    }
    public class DescriptionMgr : Singleton<DescriptionMgr>
    {
        Dictionary<string, NodeDescription> m_DescriptionDic = new Dictionary<string, NodeDescription>();
        Dictionary<int, string> m_HierachyDic = new Dictionary<int, string>();

        public NodeDescription GetNodeDescription(string name)
        {
            if (m_DescriptionDic.TryGetValue(name, out NodeDescription desc))
            {
                return desc;
            }
            return null;
        }

        public string GetHierachyDescription(int hierachy)
        {
            if (m_HierachyDic.TryGetValue(hierachy, out string desc))
            {
                return desc;
            }
            return string.Empty;
        }

        public void Load(string path)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(path);
            XmlElement root = xmlDoc.DocumentElement;

            foreach (XmlNode rootchild in root.ChildNodes)
            {
                if (rootchild.Name == "Nodes")
                {
                    foreach (XmlNode node in rootchild.ChildNodes)
                    {
                        NodeDescription desc = new NodeDescription();
                        var attr = node.Attributes["Content"];
                        if (attr != null)
                            desc.node = attr.Value;

                        foreach (XmlNode chi in node.ChildNodes)
                        {
                            var chiattr = chi.Attributes["Content"];
                            if (chiattr != null)
                                desc.SetVariable(chi.Name, chiattr.Value);
                        }

                        m_DescriptionDic[node.Name] = desc;
                    }
                }
                else if (rootchild.Name == "Hierachies")
                {
                    m_HierachyDic.Clear();
                    foreach (XmlNode node in rootchild.ChildNodes)
                    {
                        var attr = node.Attributes["Value"];
                        if (attr != null && int.TryParse(attr.Value, out int hierachy))
                        {
                            m_HierachyDic.Add(hierachy, node.InnerText);
                        }
                    }
                }
            }
        }
    }
}
