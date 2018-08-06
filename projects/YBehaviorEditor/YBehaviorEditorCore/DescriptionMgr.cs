using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace YBehavior.Editor.Core
{
    class NodeDescription
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
    class DescriptionMgr : Singleton<DescriptionMgr>
    {
        Dictionary<string, NodeDescription> m_DescriptionDic = new Dictionary<string, NodeDescription>();

        public NodeDescription GetNodeDescription(string name)
        {
            if (m_DescriptionDic.TryGetValue(name, out NodeDescription desc))
            {
                return desc;
            }
            return null;
        }
        public void Load(string path)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(path);
            XmlElement root = xmlDoc.DocumentElement;

            foreach (XmlNode node in root.ChildNodes)
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
    }
}
